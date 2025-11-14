using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using Reec.Inspection;
using Reec.Inspection.Entities;
using Reec.Inspection.HttpMessageHandler;
using Reec.Inspection.Options;
using Reec.Inspection.Services;
using Reec.Test.xUnit.Helpers;
using System.Net;

namespace Reec.Test.xUnit.HttpMessageHandlers
{
    /// <summary>
    /// Tests para LogEndpointHandler - Registro y resiliencia de llamadas HTTP externas
    /// </summary>
    public class LogEndpointHandlerTests : IDisposable
    {
        private readonly TestInspectionDbContext _dbContext;
        private readonly ReecExceptionOptions _options;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDateTimeService _dateTimeService;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;

        public LogEndpointHandlerTests()
        {
            var (context, options, serviceProvider) = TestDbContextFactory.CreateInMemoryContextWithServices(Guid.NewGuid().ToString());

            _dbContext = context;
            _options = options;
            _serviceProvider = serviceProvider;
            _dateTimeService = serviceProvider.GetRequiredService<IDateTimeService>();

            // Mock HttpContextAccessor
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = HttpContextFactory.CreateHttpContext();
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        }

        [Fact]
        public async Task SendAsync_WithSuccessfulRequest_ShouldLogEndpoint()
        {
            // Arrange
            var mockHttpMessageHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK, "{\"message\": \"success\"}");

            var handler = CreateLogEndpointHandler(mockHttpMessageHandler.Object);
            var client = new HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            _dbContext.LogEndpoints.Should().HaveCount(1);

            var logEndpoint = _dbContext.LogEndpoints.First();
            logEndpoint.ApplicationName.Should().Be(_options.ApplicationName);
            logEndpoint.Method.Should().Be("GET");
            logEndpoint.Host.Should().Be("api.example.com");
            logEndpoint.Path.Should().Be("/test");
            logEndpoint.HttpStatusCode.Should().Be((int)HttpStatusCode.OK);
            logEndpoint.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        }

        [Theory]
        [InlineData(HttpStatusCode.OK, 200)]
        [InlineData(HttpStatusCode.Created, 201)]
        [InlineData(HttpStatusCode.BadRequest, 400)]
        [InlineData(HttpStatusCode.NotFound, 404)]
        [InlineData(HttpStatusCode.InternalServerError, 500)]
        public async Task SendAsync_WithDifferentStatusCodes_ShouldLogCorrectly(HttpStatusCode statusCode, int expectedCode)
        {
            // Arrange
            var mockHttpMessageHandler = CreateMockHttpMessageHandler(statusCode);

            var handler = CreateLogEndpointHandler(mockHttpMessageHandler.Object);
            var client = new HttpClient(handler);

            // Act
            await client.GetAsync("https://api.example.com/test");

            // Assert
            var logEndpoint = _dbContext.LogEndpoints.First();
            logEndpoint.HttpStatusCode.Should().Be(expectedCode);
        }

        [Fact]
        public async Task SendAsync_WithQueryString_ShouldLogQueryString()
        {
            // Arrange
            var mockHttpMessageHandler = CreateMockHttpMessageHandler();
            var handler = CreateLogEndpointHandler(mockHttpMessageHandler.Object);
            var client = new HttpClient(handler);

            // Act
            await client.GetAsync("https://api.example.com/test?id=123&name=test");

            // Assert
            var logEndpoint = _dbContext.LogEndpoints.First();
            logEndpoint.QueryString.Should().Be("?id=123&name=test");
        }

        [Fact]
        public async Task SendAsync_WithRequestHeaders_ShouldLogHeaders()
        {
            // Arrange
            var mockHttpMessageHandler = CreateMockHttpMessageHandler();
            var handler = CreateLogEndpointHandler(mockHttpMessageHandler.Object);
            var client = new HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test");
            request.Headers.Add("Authorization", "Bearer token123");
            request.Headers.Add("X-Custom-Header", "custom-value");

            // Act
            await client.SendAsync(request);

            // Assert
            var logEndpoint = _dbContext.LogEndpoints.First();
            logEndpoint.RequestHeader.Should().NotBeNull();
            logEndpoint.RequestHeader.Should().ContainKey("Authorization");
            logEndpoint.RequestHeader.Should().ContainKey("X-Custom-Header");
        }

        [Fact]
        public async Task SendAsync_WithRequestBody_ShouldLogRequestBody()
        {
            // Arrange
            var mockHttpMessageHandler = CreateMockHttpMessageHandler();
            var handler = CreateLogEndpointHandler(mockHttpMessageHandler.Object);
            var client = new HttpClient(handler);
            var content = new StringContent("{\"name\":\"test\",\"value\":123}");

            // Act
            await client.PostAsync("https://api.example.com/test", content);

            // Assert
            var logEndpoint = _dbContext.LogEndpoints.First();
            logEndpoint.RequestBody.Should().NotBeNullOrWhiteSpace();
            logEndpoint.RequestBody.Should().Contain("test");
            logEndpoint.RequestBody.Should().Contain("123");
        }

        [Fact]
        public async Task SendAsync_WithResponseBody_ShouldLogResponseBody()
        {
            // Arrange
            var responseContent = "{\"status\":\"success\",\"data\":{\"id\":456}}";
            var mockHttpMessageHandler = CreateMockHttpMessageHandler(HttpStatusCode.OK, responseContent);
            var handler = CreateLogEndpointHandler(mockHttpMessageHandler.Object);
            var client = new HttpClient(handler);

            // Act
            await client.GetAsync("https://api.example.com/test");

            // Assert
            var logEndpoint = _dbContext.LogEndpoints.First();
            logEndpoint.ResponseBody.Should().NotBeNullOrWhiteSpace();
            logEndpoint.ResponseBody.Should().Contain("success");
            logEndpoint.ResponseBody.Should().Contain("456");
        }

        [Fact]
        public async Task SendAsync_WithRetryAttempts_ShouldLogRetryCount()
        {
            // Arrange
            var mockHttpMessageHandler = CreateMockHttpMessageHandler();
            var handler = CreateLogEndpointHandler(mockHttpMessageHandler.Object);
            var client = new HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test");

            // Simular que es el tercer intento
            var retryKey = new HttpRequestOptionsKey<int>("RetryAttempts");
            request.Options.Set(retryKey, 3);

            // Act
            await client.SendAsync(request);

            // Assert
            var logEndpoint = _dbContext.LogEndpoints.First();
            logEndpoint.Retry.Should().Be(3);
        }

        [Fact]
        public async Task SendAsync_ShouldCaptureExecutionDuration()
        {
            // Arrange
            var mock = new Mock<HttpMessageHandler>();
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(async (HttpRequestMessage request, CancellationToken ct) =>
                {
                    await Task.Delay(100); // Simular latencia
                    var response = new HttpResponseMessage { StatusCode = HttpStatusCode.OK };
                    response.RequestMessage = request;
                    return response;
                });

            var handler = CreateLogEndpointHandler(mock.Object);
            var client = new HttpClient(handler);

            // Act
            await client.GetAsync("https://api.example.com/test");

            // Assert
            var logEndpoint = _dbContext.LogEndpoints.First();
            logEndpoint.Duration.Should().BeGreaterThan(TimeSpan.FromMilliseconds(50));
            logEndpoint.Duration.TotalMilliseconds.Should().BeGreaterOrEqualTo(100);
        }

        [Fact]
        public async Task SendAsync_WhenDisabled_ShouldNotLog()
        {
            // Arrange
            _options.EnableGlobalDbSave = false;
            var mockHttpMessageHandler = CreateMockHttpMessageHandler();
            var handler = CreateLogEndpointHandler(mockHttpMessageHandler.Object);
            var client = new HttpClient(handler);

            // Act
            await client.GetAsync("https://api.example.com/test");

            // Assert
            _dbContext.LogEndpoints.Should().BeEmpty();
        }

        [Fact]
        public async Task SendAsync_WhenLogEndpointDisabled_ShouldNotLog()
        {
            // Arrange
            _options.LogEndpoint.IsSaveDB = false;
            var mockHttpMessageHandler = CreateMockHttpMessageHandler();
            var handler = CreateLogEndpointHandler(mockHttpMessageHandler.Object);
            var client = new HttpClient(handler);

            // Act
            await client.GetAsync("https://api.example.com/test");

            // Assert
            _dbContext.LogEndpoints.Should().BeEmpty();
        }

        [Fact]
        public async Task SendAsync_WithHttpsRequest_ShouldLogScheme()
        {
            // Arrange
            var mockHttpMessageHandler = CreateMockHttpMessageHandler();
            var handler = CreateLogEndpointHandler(mockHttpMessageHandler.Object);
            var client = new HttpClient(handler);

            // Act
            await client.GetAsync("https://api.example.com/test");

            // Assert
            var logEndpoint = _dbContext.LogEndpoints.First();
            logEndpoint.Schema.Should().Be("https");
            logEndpoint.Port.Should().Be(443);
        }

        [Fact]
        public async Task SendAsync_ShouldSetCreateDate()
        {
            // Arrange
            var mockHttpMessageHandler = CreateMockHttpMessageHandler();
            var handler = CreateLogEndpointHandler(mockHttpMessageHandler.Object);
            var client = new HttpClient(handler);

            var beforeRequest = _dateTimeService.Now;

            // Act
            await client.GetAsync("https://api.example.com/test");

            var afterRequest = _dateTimeService.Now;

            // Assert
            var logEndpoint = _dbContext.LogEndpoints.First();
            logEndpoint.CreateDate.Should().BeOnOrAfter(beforeRequest);
            logEndpoint.CreateDate.Should().BeOnOrBefore(afterRequest);
            logEndpoint.CreateDateOnly.Should().Be(DateOnly.FromDateTime(_dateTimeService.Now));
        }

        private Mock<HttpMessageHandler> CreateMockHttpMessageHandler(HttpStatusCode statusCode = HttpStatusCode.OK, string responseContent = null)
        {
            var mock = new Mock<HttpMessageHandler>();
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage request, CancellationToken ct) =>
                {
                    var response = new HttpResponseMessage
                    {
                        StatusCode = statusCode,
                        Content = responseContent != null ? new StringContent(responseContent) : null
                    };
                    response.RequestMessage = request;
                    return response;
                });
            return mock;
        }

        private LogEndpointHandler CreateLogEndpointHandler(HttpMessageHandler innerHandler)
        {
            // Create a custom ServiceProvider with mocked HttpContextAccessor
            var services = new ServiceCollection();

            // Add all existing services
            var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
            services.AddSingleton(scopeFactory);
            services.AddSingleton(_options);
            services.AddSingleton(_dateTimeService);
            services.AddSingleton(_mockHttpContextAccessor.Object);
            services.AddScoped(_ => _dbContext);
            services.AddScoped<InspectionDbContext>(_ => _dbContext);
            services.AddScoped<IDbContextService>(sp => 
                new DbContextService<TestInspectionDbContext>(_dbContext));

            var customServiceProvider = services.BuildServiceProvider();
            var customScopeFactory = customServiceProvider.GetRequiredService<IServiceScopeFactory>();

            var handler = new LogEndpointHandler(customScopeFactory, _options, _dateTimeService)
            {
                InnerHandler = innerHandler
            };

            return handler;
        }

        public void Dispose()
        {
            TestDbContextFactory.CleanupContextWithServices(_dbContext, _serviceProvider);
        }
    }
}
