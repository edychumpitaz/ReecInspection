using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Reec.Inspection.Middlewares;
using Reec.Inspection.Options;
using Reec.Inspection.Services;
using Reec.Test.xUnit.Helpers;

namespace Reec.Test.xUnit.Middlewares
{
    /// <summary>
    /// Tests para LogAuditMiddleware - Auditoría de peticiones HTTP
    /// </summary>
    public class LogAuditMiddlewareTests : IDisposable
    {
        private readonly TestInspectionDbContext _dbContext;
        private readonly ReecExceptionOptions _options;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger<LogAuditMiddleware> _logger;
        private readonly LogAuditMiddleware _middleware;
        private readonly IServiceProvider _serviceProvider;

        public LogAuditMiddlewareTests()
        {
            // Usar el método con servicios completos
            var (context, options, serviceProvider) = TestDbContextFactory.CreateInMemoryContextWithServices(Guid.NewGuid().ToString());

            _dbContext = context;
            _options = options;
            _serviceProvider = serviceProvider;
            _dateTimeService = serviceProvider.GetRequiredService<IDateTimeService>();

            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            _logger = loggerFactory.CreateLogger<LogAuditMiddleware>();

            var dbContextService = serviceProvider.GetRequiredService<IDbContextService>();
            _middleware = new LogAuditMiddleware(_logger, dbContextService, _options, _dateTimeService);
        }

        [Fact]
        public async Task InvokeAsync_WithValidRequest_ShouldLogAudit()
        {
            // Arrange
            var context = HttpContextFactory.CreateHttpContext("/api/test", "GET");
            var requestDelegate = new RequestDelegate(_ => Task.CompletedTask);

            // Act
            await _middleware.InvokeAsync(context, requestDelegate);

            // Assert
            _dbContext.LogAudits.Should().HaveCount(1);
            var audit = _dbContext.LogAudits.First();
            audit.Path.Should().Be("/api/test");
            audit.Method.Should().Be("GET");
        }

        [Theory]
        [InlineData("/swagger/index.html")]
        [InlineData("/health")]
        [InlineData("/api/swagger/docs")]
        public async Task InvokeAsync_WithExcludedPath_ShouldNotLog(string path)
        {
            // Arrange
            var context = HttpContextFactory.CreateHttpContext(path);
            var requestDelegate = new RequestDelegate(_ => Task.CompletedTask);

            // Act
            await _middleware.InvokeAsync(context, requestDelegate);

            // Assert
            _dbContext.LogAudits.Should().BeEmpty();
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("DELETE")]
        [InlineData("PATCH")]
        public async Task InvokeAsync_WithDifferentMethods_ShouldLogCorrectMethod(string method)
        {
            // Arrange
            var context = HttpContextFactory.CreateHttpContext("/api/test", method);
            var requestDelegate = new RequestDelegate(_ => Task.CompletedTask);

            // Act
            await _middleware.InvokeAsync(context, requestDelegate);

            // Assert
            var audit = _dbContext.LogAudits.First();
            audit.Method.Should().Be(method);
        }

        [Fact]
        public async Task InvokeAsync_WithRequestHeaders_ShouldLogHeaders()
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                { "Authorization", "Bearer token123" },
                { "Content-Type", "application/json" },
                { "X-Custom-Header", "custom-value" }
            };
            var context = HttpContextFactory.CreateHttpContext(headers: headers);
            var requestDelegate = new RequestDelegate(_ => Task.CompletedTask);

            // Act
            await _middleware.InvokeAsync(context, requestDelegate);

            // Assert
            var audit = _dbContext.LogAudits.First();
            audit.RequestHeader.Should().NotBeNull();
            audit.RequestHeader.Should().ContainKey("Authorization");
            audit.RequestHeader.Should().ContainKey("Content-Type");
        }

        [Fact]
        public async Task InvokeAsync_WithQueryString_ShouldLogQueryString()
        {
            // Arrange
            var context = HttpContextFactory.CreateHttpContext("/api/test");
            context.Request.QueryString = new QueryString("?id=123&name=test");
            var requestDelegate = new RequestDelegate(_ => Task.CompletedTask);

            // Act
            await _middleware.InvokeAsync(context, requestDelegate);

            // Assert
            var audit = _dbContext.LogAudits.First();
            audit.QueryString.Should().Be("?id=123&name=test");
        }

        [Fact]
        public async Task InvokeAsync_ShouldCaptureStatusCode()
        {
            // Arrange
            var context = HttpContextFactory.CreateHttpContext();
            var requestDelegate = new RequestDelegate(ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status201Created;
                return Task.CompletedTask;
            });

            // Act
            await _middleware.InvokeAsync(context, requestDelegate);

            // Assert
            var audit = _dbContext.LogAudits.First();
            audit.HttpStatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        }

        [Fact]
        public async Task InvokeAsync_ShouldCaptureDuration()
        {
            // Arrange
            var context = HttpContextFactory.CreateHttpContext();
            var requestDelegate = new RequestDelegate(async _ =>
            {
                await Task.Delay(100);
            });

            // Act
            await _middleware.InvokeAsync(context, requestDelegate);

            // Assert
            var audit = _dbContext.LogAudits.First();
            audit.Duration.Should().NotBeNull();
            audit.Duration!.Value.TotalMilliseconds.Should().BeGreaterOrEqualTo(100);
        }

        [Fact]
        public async Task InvokeAsync_ShouldCaptureTraceIdentifier()
        {
            // Arrange
            var context = HttpContextFactory.CreateHttpContext();
            var expectedTraceId = context.TraceIdentifier;
            var requestDelegate = new RequestDelegate(_ => Task.CompletedTask);

            // Act
            await _middleware.InvokeAsync(context, requestDelegate);

            // Assert
            var audit = _dbContext.LogAudits.First();
            audit.TraceIdentifier.Should().Be(expectedTraceId);
            audit.RequestId.Should().Be(expectedTraceId);
        }

        [Fact]
        public async Task InvokeAsync_ShouldCaptureHostInformation()
        {
            // Arrange
            var context = HttpContextFactory.CreateHttpContext();
            var requestDelegate = new RequestDelegate(_ => Task.CompletedTask);

            // Act
            await _middleware.InvokeAsync(context, requestDelegate);

            // Assert
            var audit = _dbContext.LogAudits.First();
            audit.Host.Should().Be("localhost");
            audit.Port.Should().Be(5000);
            audit.HostPort.Should().Be("localhost:5000");
        }

        [Fact]
        public async Task InvokeAsync_ShouldCaptureIpAddress()
        {
            // Arrange
            var context = HttpContextFactory.CreateHttpContext();
            var requestDelegate = new RequestDelegate(_ => Task.CompletedTask);

            // Act
            await _middleware.InvokeAsync(context, requestDelegate);

            // Assert
            var audit = _dbContext.LogAudits.First();
            audit.IpAddress.Should().Be("127.0.0.1");
        }

        [Fact]
        public async Task InvokeAsync_ShouldSetApplicationName()
        {
            // Arrange
            var context = HttpContextFactory.CreateHttpContext();
            var requestDelegate = new RequestDelegate(_ => Task.CompletedTask);

            // Act
            await _middleware.InvokeAsync(context, requestDelegate);

            // Assert
            var audit = _dbContext.LogAudits.First();
            audit.ApplicationName.Should().Be(_options.ApplicationName);
        }

        public void Dispose()
        {
            TestDbContextFactory.CleanupContextWithServices(_dbContext, _serviceProvider);
        }
    }
}
