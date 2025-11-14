using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Reec.Inspection;
using Reec.Inspection.Middlewares;
using Reec.Inspection.Options;
using Reec.Inspection.Services;
using Reec.Test.xUnit.Helpers;
using System.Text.Json;
using static Reec.Inspection.ReecEnums;

namespace Reec.Test.xUnit.Middlewares
{
    /// <summary>
    /// Tests para LogHttpMiddleware - Captura de excepciones y respuestas de error
    /// </summary>
    public class LogHttpMiddlewareTests : IDisposable
    {
        private readonly TestInspectionDbContext _dbContext;
        private readonly ReecExceptionOptions _options;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger<LogHttpMiddleware> _logger;
        private readonly LogHttpMiddleware _middleware;
        private readonly IServiceProvider _serviceProvider;

        public LogHttpMiddlewareTests()
        {
            // Usar el método con servicios completos
            var (context, options, serviceProvider) = TestDbContextFactory.CreateInMemoryContextWithServices(Guid.NewGuid().ToString());

            _dbContext = context;
            _options = options;
            _serviceProvider = serviceProvider;
            _dateTimeService = serviceProvider.GetRequiredService<IDateTimeService>();

            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            _logger = loggerFactory.CreateLogger<LogHttpMiddleware>();

            var dbContextService = serviceProvider.GetRequiredService<IDbContextService>();
            _middleware = new LogHttpMiddleware(_logger, dbContextService, _options, _dateTimeService);
        }

        [Fact]
        public async Task InvokeAsync_WithNoException_ShouldNotLog()
        {
            // Arrange
            var context = HttpContextFactory.CreateHttpContext();
            var requestDelegate = new RequestDelegate(_ => Task.CompletedTask);

            // Act
            await _middleware.InvokeAsync(context, requestDelegate);

            // Assert
            _dbContext.LogHttps.Should().BeEmpty();
        }

        [Fact]
        public async Task InvokeAsync_WithReecException_ShouldLogAndReturn400()
        {
            // Arrange
            var context = HttpContextFactory.CreateHttpContext();
            var requestDelegate = new RequestDelegate(_ =>
                throw new ReecException(Category.Warning, "Campo requerido"));

            // Act
            await _middleware.InvokeAsync(context, requestDelegate);

            // Assert
            context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
            _dbContext.LogHttps.Should().HaveCount(1);

            var log = _dbContext.LogHttps.First();
            log.Category.Should().Be(Category.Warning);
            log.HttpStatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task InvokeAsync_WithUnhandledException_ShouldLogAndReturn500()
        {
            // Arrange
            var context = HttpContextFactory.CreateHttpContext();
            var requestDelegate = new RequestDelegate(_ =>
                throw new InvalidOperationException("Error no controlado"));

            // Act
            await _middleware.InvokeAsync(context, requestDelegate);

            // Assert
            context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            _dbContext.LogHttps.Should().HaveCount(1);

            var log = _dbContext.LogHttps.First();
            log.Category.Should().Be(Category.InternalServerError);
            log.ExceptionMessage.Should().Contain("Error no controlado");
        }

        [Fact]
        public async Task InvokeAsync_WithProblemDetailsEnabled_ShouldReturnProblemDetails()
        {
            // Arrange
            _options.EnableProblemDetails = true;
            var context = HttpContextFactory.CreateHttpContext();
            var requestDelegate = new RequestDelegate(_ =>
                throw new ReecException(Category.Warning, "Validación fallida"));

            // Act
            await _middleware.InvokeAsync(context, requestDelegate);

            // Assert
            context.Response.Headers.Should().ContainKey("EnableProblemDetails");
            context.Response.Headers["EnableProblemDetails"].ToString().Should().Be("true");

            var responseBody = await HttpContextFactory.ReadResponseBodyAsync(context);
            responseBody.Should().Contain("Validación fallida");
        }

        [Fact]
        public async Task InvokeAsync_WithAuthenticatedUser_ShouldLogUsername()
        {
            // Arrange
            var user = HttpContextFactory.CreateAuthenticatedUser("testuser");
            var context = HttpContextFactory.CreateHttpContext(user: user);
            var requestDelegate = new RequestDelegate(_ =>
                throw new ReecException(Category.Warning, "Test"));

            // Act
            await _middleware.InvokeAsync(context, requestDelegate);

            // Assert
            var log = _dbContext.LogHttps.First();
            log.CreateUser.Should().Be("testuser");
        }

        [Theory]
        [InlineData(Category.Warning)]
        [InlineData(Category.BusinessLogic)]
        [InlineData(Category.BusinessLogicLegacy)]
        public async Task InvokeAsync_WithBusinessCategories_ShouldReturn400(Category category)
        {
            // Arrange
            var context = HttpContextFactory.CreateHttpContext();
            var requestDelegate = new RequestDelegate(_ =>
                throw new ReecException(category, "Business error"));

            // Act
            await _middleware.InvokeAsync(context, requestDelegate);

            // Assert
            context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task InvokeAsync_WithRequestBody_ShouldLogRequestBody()
        {
            // Arrange
            var requestBody = JsonSerializer.Serialize(new { Name = "Test", Value = 123 });
            var context = HttpContextFactory.CreateHttpContext(requestBody: requestBody);
            var requestDelegate = new RequestDelegate(_ =>
                throw new ReecException(Category.Warning, "Test"));

            // Act
            await _middleware.InvokeAsync(context, requestDelegate);

            // Assert
            var log = _dbContext.LogHttps.First();
            log.RequestBody.Should().Contain("Test");
            log.RequestBody.Should().Contain("123");
        }

        [Fact]
        public async Task InvokeAsync_WithInnerException_ShouldLogInnerException()
        {
            // Arrange
            var context = HttpContextFactory.CreateHttpContext();
            var innerException = new ArgumentNullException("parameter");
            var requestDelegate = new RequestDelegate(_ =>
                throw new ReecException(Category.BusinessLogic, "Outer", "Message", innerException));

            // Act
            await _middleware.InvokeAsync(context, requestDelegate);

            // Assert
            var log = _dbContext.LogHttps.First();
            log.InnerExceptionMessage.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task InvokeAsync_ShouldCaptureDuration()
        {
            // Arrange
            var context = HttpContextFactory.CreateHttpContext();
            var requestDelegate = new RequestDelegate(async _ =>
            {
                await Task.Delay(100);
                throw new ReecException(Category.Warning, "Test");
            });

            // Act
            await _middleware.InvokeAsync(context, requestDelegate);

            // Assert
            var log = _dbContext.LogHttps.First();
            log.Duration.Should().NotBeNull();
            log.Duration!.Value.TotalMilliseconds.Should().BeGreaterOrEqualTo(100);
        }

        public void Dispose()
        {
            TestDbContextFactory.CleanupContextWithServices(_dbContext, _serviceProvider);
        }
    }
}
