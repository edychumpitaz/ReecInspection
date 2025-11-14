using Microsoft.Extensions.DependencyInjection;
using Reec.Inspection.Workers;
using Reec.Test.xUnit.Helpers;
using static Reec.Inspection.ReecEnums;

namespace Reec.Test.xUnit.Workers
{
    /// <summary>
    /// Tests para IWorker - Ejecución de tareas en segundo plano
    /// </summary>
    public class WorkerTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly TestInspectionDbContext _dbContext;

        public WorkerTests()
        {
            var (context, options, serviceProvider) = TestDbContextFactory.CreateInMemoryContextWithWorker(Guid.NewGuid().ToString());

            _dbContext = context;
            _serviceProvider = (ServiceProvider)serviceProvider;
        }

        [Fact]
        public async Task ExecuteAsync_WithSuccessfulExecution_ShouldLogSuccess()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var worker = scope.ServiceProvider.GetRequiredService<IWorker>();
            worker.NameJob = "TestJob";
            worker.CreateUser = "TestUser";
            worker.IsLightExecution = false;

            worker.RunFunction = _ => Task.FromResult("Ejecución exitosa");

            // Act
            await worker.ExecuteAsync();

            // Assert
            _dbContext.LogJobs.Should().HaveCountGreaterOrEqualTo(2); // Enqueued + Succeeded

            var successLog = _dbContext.LogJobs
                .FirstOrDefault(j => j.StateJob == StateJob.Succeeded);

            successLog.Should().NotBeNull();
            successLog!.NameJob.Should().Be("TestJob");
            successLog.CreateUser.Should().Be("TestUser");
            successLog.Message.Should().Be("Ejecución exitosa");
        }

        [Fact]
        public async Task ExecuteAsync_WithException_ShouldLogFailure()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var worker = scope.ServiceProvider.GetRequiredService<IWorker>();
            worker.NameJob = "FailingJob";
            worker.IsLightExecution = false;

            worker.RunFunction = _ => throw new InvalidOperationException("Error intencional");

            // Act
            await worker.ExecuteAsync();

            // Assert
            var failedLog = _dbContext.LogJobs
                .FirstOrDefault(j => j.StateJob == StateJob.Failed);

            failedLog.Should().NotBeNull();
            failedLog!.Exception.Should().Contain("Error intencional");
            failedLog.StackTrace.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task ExecuteAsync_WithLightExecution_ShouldOnlyLogErrors()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var worker = scope.ServiceProvider.GetRequiredService<IWorker>();
            worker.NameJob = "LightJob";
            worker.IsLightExecution = true;

            worker.RunFunction = _ => Task.FromResult("Success");

            // Act
            await worker.ExecuteAsync();

            // Assert
            // En modo light, solo se registran errores
            _dbContext.LogJobs.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteAsync_WithLightExecutionAndError_ShouldLogError()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var worker = scope.ServiceProvider.GetRequiredService<IWorker>();
            worker.NameJob = "LightJobWithError";
            worker.IsLightExecution = true;

            worker.RunFunction = _ => throw new Exception("Error en light mode");

            // Act
            await worker.ExecuteAsync();

            // Assert
            _dbContext.LogJobs.Should().HaveCount(1);
            var log = _dbContext.LogJobs.First();
            log.StateJob.Should().Be(StateJob.Failed);
        }

        [Fact]
        public async Task ExecuteAsync_WithCustomExceptionHandler_ShouldInvokeHandler()
        {
            // Arrange
            bool handlerInvoked = false;
            using var scope = _serviceProvider.CreateScope();
            var worker = scope.ServiceProvider.GetRequiredService<IWorker>();
            worker.NameJob = "JobWithHandler";
            worker.IsLightExecution = false;

            worker.RunFunction = _ => throw new Exception("Test exception");
            worker.RunFunctionException = (_, ex) =>
            {
                handlerInvoked = true;
                ex.Message.Should().Be("Test exception");
                return Task.CompletedTask;
            };

            // Act
            await worker.ExecuteAsync();

            // Assert
            handlerInvoked.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteAsync_WithDelay_ShouldWaitBeforeExecution()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var worker = scope.ServiceProvider.GetRequiredService<IWorker>();
            worker.NameJob = "DelayedJob";
            worker.Delay = TimeSpan.FromMilliseconds(200);
            worker.IsLightExecution = false;

            var startTime = DateTime.UtcNow;
            worker.RunFunction = _ => Task.FromResult("Completed");

            // Act
            await worker.ExecuteAsync();

            // Assert
            var elapsed = DateTime.UtcNow - startTime;
            elapsed.TotalMilliseconds.Should().BeGreaterOrEqualTo(180); // Tolerancia de 20ms
        }

        [Fact]
        public async Task ExecuteAsync_ShouldSetTraceIdentifier()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var worker = scope.ServiceProvider.GetRequiredService<IWorker>();
            worker.NameJob = "TraceJob";
            worker.TraceIdentifier = "custom-trace-123";
            worker.IsLightExecution = false;

            worker.RunFunction = _ => Task.FromResult("Done");

            // Act
            await worker.ExecuteAsync();

            // Assert
            var log = _dbContext.LogJobs.FirstOrDefault(j => j.StateJob == StateJob.Succeeded);
            log.Should().NotBeNull();
            log!.TraceIdentifier.Should().Be("custom-trace-123");
        }

        [Fact]
        public async Task ExecuteAsync_WithCancellationToken_ShouldRespectCancellation()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var worker = scope.ServiceProvider.GetRequiredService<IWorker>();
            worker.NameJob = "CancellableJob";
            worker.IsLightExecution = false;

            var cts = new CancellationTokenSource();
            cts.CancelAfter(100);

            worker.RunFunction = async _ =>
            {
                await Task.Delay(5000, cts.Token);
                return "Completed";
            };

            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await worker.ExecuteAsync(cts.Token));
        }

        [Fact]
        public async Task ExecuteAsync_MultipleExecutions_ShouldLogSeparately()
        {
            // Arrange - Crear 2 scopes independientes para evitar compartir estado
            using var scope1 = _serviceProvider.CreateScope();
            var worker1 = scope1.ServiceProvider.GetRequiredService<IWorker>();
            worker1.NameJob = "Job1";
            worker1.IsLightExecution = false;
            worker1.RunFunction = _ => Task.FromResult("Result 1");

            using var scope2 = _serviceProvider.CreateScope();
            var worker2 = scope2.ServiceProvider.GetRequiredService<IWorker>();
            worker2.NameJob = "Job2";
            worker2.IsLightExecution = false;
            worker2.RunFunction = _ => Task.FromResult("Result 2");

            // Act
            await worker1.ExecuteAsync();
            await worker2.ExecuteAsync();

            // Assert
            _dbContext.LogJobs.Should().HaveCountGreaterOrEqualTo(4); // 2 jobs x (Enqueued + Succeeded)

            var job1Logs = _dbContext.LogJobs.Where(j => j.NameJob == "Job1").ToList();
            var job2Logs = _dbContext.LogJobs.Where(j => j.NameJob == "Job2").ToList();

            job1Logs.Should().HaveCountGreaterOrEqualTo(2);
            job2Logs.Should().HaveCountGreaterOrEqualTo(2);
        }

        public void Dispose()
        {
            TestDbContextFactory.CleanupContextWithServices(_dbContext, _serviceProvider);
        }
    }
}
