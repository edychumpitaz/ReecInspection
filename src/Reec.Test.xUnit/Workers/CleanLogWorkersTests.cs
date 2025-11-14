using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Reec.Inspection.Entities;
using Reec.Inspection.Options;
using Reec.Inspection.Services;
using Reec.Inspection.Workers;
using Reec.Test.xUnit.Helpers;

namespace Reec.Test.xUnit.Workers
{
    /// <summary>
    /// Tests para CleanLog*Workers - Workers de limpieza automática de logs
    /// </summary>
    public class CleanLogWorkersTests : IDisposable
    {
        private readonly TestInspectionDbContext _dbContext;
        private readonly ReecExceptionOptions _options;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDateTimeService _dateTimeService;

        public CleanLogWorkersTests()
        {
            var (context, options, serviceProvider) = TestDbContextFactory.CreateInMemoryContextWithWorker(Guid.NewGuid().ToString());

            _dbContext = context;
            _options = options;
            _serviceProvider = serviceProvider;
            _dateTimeService = serviceProvider.GetRequiredService<IDateTimeService>();

            // Configurar opciones para limpieza
            _options.LogAudit.EnableClean = true;
            _options.LogAudit.CronValue = "* * * * *"; // Cada minuto (para testing)
            _options.LogAudit.DeleteDays = 7;
            _options.LogAudit.DeleteBatch = 100;

            _options.LogHttp.EnableClean = true;
            _options.LogHttp.CronValue = "* * * * *";
            _options.LogHttp.DeleteDays = 7;
            _options.LogHttp.DeleteBatch = 100;

            _options.LogEndpoint.EnableClean = true;
            _options.LogEndpoint.CronValue = "* * * * *";
            _options.LogEndpoint.DeleteDays = 7;
            _options.LogEndpoint.DeleteBatch = 100;

            _options.LogJob.EnableClean = true;
            _options.LogJob.CronValue = "* * * * *";
            _options.LogJob.DeleteDays = 7;
            _options.LogJob.DeleteBatch = 100;
        }

        #region CleanLogAuditWorker Tests

        [Fact]
        public async Task CleanLogAuditWorker_ShouldDeleteOldRecords()
        {
            // Arrange
            var oldDate = _dateTimeService.Now.AddDays(-10); // Más viejo que DeleteDays (7)
            var recentDate = _dateTimeService.Now.AddDays(-3); // Más reciente que DeleteDays

            await SeedLogAuditData(oldDate, 5);
            await SeedLogAuditData(recentDate, 3);

            using var scope = _serviceProvider.CreateScope();
            var worker = scope.ServiceProvider.GetRequiredService<IWorker>();

            // Act
            worker.NameJob = nameof(CleanLogAuditWorker);
            worker.CreateUser = "Test";
            worker.IsLightExecution = false;
            worker.RunFunction = service => CleanLogAuditWorker_Process(service, _options, _dateTimeService);

            await worker.ExecuteAsync();

            // Assert
            var remainingLogs = await _dbContext.LogAudits.CountAsync();
            remainingLogs.Should().Be(3); // Solo los recientes deben quedar
        }

        [Fact]
        public async Task CleanLogAuditWorker_WhenDisabled_ShouldNotDelete()
        {
            // Arrange
            _options.LogAudit.EnableClean = false;
            var oldDate = _dateTimeService.Now.AddDays(-10);
            await SeedLogAuditData(oldDate, 5);

            var cts = new CancellationTokenSource();
            cts.CancelAfter(100); // Cancelar después de 100ms

            var worker = new CleanLogAuditWorker(_serviceProvider.GetRequiredService<IServiceScopeFactory>(), _options, _dateTimeService);

            // Act
            await worker.StartAsync(cts.Token);
            await Task.Delay(200); // Esperar un poco más que el CancellationToken
            await worker.StopAsync(CancellationToken.None);

            // Assert
            var remainingLogs = await _dbContext.LogAudits.CountAsync();
            remainingLogs.Should().Be(5); // Todos deben quedar porque está deshabilitado
        }

        [Fact]
        public async Task CleanLogAuditWorker_ShouldRespectDeleteBatch()
        {
            // Arrange
            _options.LogAudit.DeleteBatch = 3; // Borrar de 3 en 3
            var oldDate = _dateTimeService.Now.AddDays(-10);
            await SeedLogAuditData(oldDate, 10);

            using var scope = _serviceProvider.CreateScope();
            var worker = scope.ServiceProvider.GetRequiredService<IWorker>();

            // Act
            worker.NameJob = nameof(CleanLogAuditWorker);
            worker.RunFunction = service => CleanLogAuditWorker_Process(service, _options, _dateTimeService);
            await worker.ExecuteAsync();

            // Assert
            var remainingLogs = await _dbContext.LogAudits.CountAsync();
            remainingLogs.Should().Be(0); // Debe borrar todos, pero en lotes de 3
        }

        #endregion

        #region CleanLogHttpWorker Tests

        [Fact]
        public async Task CleanLogHttpWorker_ShouldDeleteOldRecords()
        {
            // Arrange
            var oldDate = _dateTimeService.Now.AddDays(-10);
            var recentDate = _dateTimeService.Now.AddDays(-3);

            await SeedLogHttpData(oldDate, 5);
            await SeedLogHttpData(recentDate, 3);

            using var scope = _serviceProvider.CreateScope();
            var worker = scope.ServiceProvider.GetRequiredService<IWorker>();

            // Act
            worker.NameJob = nameof(CleanLogHttpWorker);
            worker.CreateUser = "Test";
            worker.IsLightExecution = false;
            worker.RunFunction = service => CleanLogHttpWorker_Process(service, _options, _dateTimeService);

            await worker.ExecuteAsync();

            // Assert
            var remainingLogs = await _dbContext.LogHttps.CountAsync();
            remainingLogs.Should().Be(3);
        }

        [Fact]
        public async Task CleanLogHttpWorker_ShouldOnlyDeleteForCurrentApplication()
        {
            // Arrange
            var oldDate = _dateTimeService.Now.AddDays(-10);

            // Logs de la aplicación actual
            await SeedLogHttpData(oldDate, 5);

            // Logs de otra aplicación
            await _dbContext.LogHttps.AddRangeAsync(
                Enumerable.Range(1, 3).Select(i => new LogHttp
                {
                    ApplicationName = "OtherApp",
                    CreateDate = oldDate,
                    CreateDateOnly = DateOnly.FromDateTime(oldDate),
                    Path = $"/other/{i}",
                    Method = "GET",
                    HttpStatusCode = System.Net.HttpStatusCode.OK
                }));
            await _dbContext.SaveChangesAsync();

            using var scope = _serviceProvider.CreateScope();
            var worker = scope.ServiceProvider.GetRequiredService<IWorker>();

            // Act
            worker.NameJob = nameof(CleanLogHttpWorker);
            worker.RunFunction = service => CleanLogHttpWorker_Process(service, _options, _dateTimeService);
            await worker.ExecuteAsync();

            // Assert
            var remainingOurApp = await _dbContext.LogHttps.CountAsync(x => x.ApplicationName == _options.ApplicationName);
            var remainingOtherApp = await _dbContext.LogHttps.CountAsync(x => x.ApplicationName == "OtherApp");

            remainingOurApp.Should().Be(0); // Nuestra app debe estar limpia
            remainingOtherApp.Should().Be(3); // Otra app debe quedar intacta
        }

        #endregion

        #region CleanLogEndpointWorker Tests

        [Fact]
        public async Task CleanLogEndpointWorker_ShouldDeleteOldRecords()
        {
            // Arrange
            var oldDate = _dateTimeService.Now.AddDays(-10);
            var recentDate = _dateTimeService.Now.AddDays(-3);

            await SeedLogEndpointData(oldDate, 5);
            await SeedLogEndpointData(recentDate, 3);

            using var scope = _serviceProvider.CreateScope();
            var worker = scope.ServiceProvider.GetRequiredService<IWorker>();

            // Act
            worker.NameJob = nameof(CleanLogEndpointWorker);
            worker.CreateUser = "Test";
            worker.IsLightExecution = false;
            worker.RunFunction = service => CleanLogEndpointWorker_Process(service, _options, _dateTimeService);

            await worker.ExecuteAsync();

            // Assert
            var remainingLogs = await _dbContext.LogEndpoints.CountAsync();
            remainingLogs.Should().Be(3);
        }

        [Fact]
        public async Task CleanLogEndpointWorker_ShouldLogJobExecution()
        {
            // Arrange
            var oldDate = _dateTimeService.Now.AddDays(-10);
            await SeedLogEndpointData(oldDate, 5);

            using var scope = _serviceProvider.CreateScope();
            var worker = scope.ServiceProvider.GetRequiredService<IWorker>();

            // Act
            worker.NameJob = nameof(CleanLogEndpointWorker);
            worker.IsLightExecution = false;
            worker.RunFunction = service => CleanLogEndpointWorker_Process(service, _options, _dateTimeService);

            await worker.ExecuteAsync();

            // Assert
            var jobLogs = await _dbContext.LogJobs
                .Where(j => j.NameJob == nameof(CleanLogEndpointWorker))
                .ToListAsync();

            jobLogs.Should().NotBeEmpty();
            jobLogs.Should().Contain(j => j.StateJob == Reec.Inspection.ReecEnums.StateJob.Succeeded);
        }

        #endregion

        #region CleanLogJobWorker Tests

        [Fact]
        public async Task CleanLogJobWorker_ShouldDeleteOldRecords()
        {
            // Arrange
            var oldDate = _dateTimeService.Now.AddDays(-10);
            var recentDate = _dateTimeService.Now.AddDays(-3);

            await SeedLogJobData(oldDate, 5);
            await SeedLogJobData(recentDate, 3);

            using var scope = _serviceProvider.CreateScope();
            var worker = scope.ServiceProvider.GetRequiredService<IWorker>();

            // Act
            worker.NameJob = "TestCleanLogJobWorker"; // Nombre diferente para no confundir
            worker.CreateUser = "Test";
            worker.IsLightExecution = false;
            worker.RunFunction = service => CleanLogJobWorker_Process(service, _options, _dateTimeService);

            await worker.ExecuteAsync();

            // Assert
            // Contamos solo los logs antiguos que deberían haberse borrado
            var oldLogs = await _dbContext.LogJobs
                .Where(j => j.CreateDateOnly <= DateOnly.FromDateTime(oldDate.AddDays(1)))
                .Where(j => j.NameJob != "TestCleanLogJobWorker") // Excluir el job actual
                .CountAsync();

            oldLogs.Should().Be(0); // Logs viejos deben estar borrados
        }

        [Fact]
        public async Task CleanLogJobWorker_WithLargeBatch_ShouldDeleteAll()
        {
            // Arrange
            _options.LogJob.DeleteBatch = 1000; // Batch grande
            var oldDate = _dateTimeService.Now.AddDays(-10);
            await SeedLogJobData(oldDate, 50);

            using var scope = _serviceProvider.CreateScope();
            var worker = scope.ServiceProvider.GetRequiredService<IWorker>();

            // Act
            worker.NameJob = "TestCleanLogJobWorker";
            worker.RunFunction = service => CleanLogJobWorker_Process(service, _options, _dateTimeService);
            await worker.ExecuteAsync();

            // Assert
            var oldLogs = await _dbContext.LogJobs
                .Where(j => j.CreateDateOnly <= DateOnly.FromDateTime(_dateTimeService.Now.AddDays(-_options.LogJob.DeleteDays)))
                .Where(j => j.NameJob != "TestCleanLogJobWorker")
                .CountAsync();

            oldLogs.Should().Be(0);
        }

        #endregion

        #region Helper Methods - Seed Data

        private async Task SeedLogAuditData(DateTime createDate, int count)
        {
            var logs = Enumerable.Range(1, count).Select(i => new LogAudit
            {
                ApplicationName = _options.ApplicationName,
                CreateDate = createDate,
                CreateDateOnly = DateOnly.FromDateTime(createDate),
                Path = $"/api/test/{i}",
                Method = "GET",
                HttpStatusCode = System.Net.HttpStatusCode.OK
            });

            await _dbContext.LogAudits.AddRangeAsync(logs);
            await _dbContext.SaveChangesAsync();
        }

        private async Task SeedLogHttpData(DateTime createDate, int count)
        {
            var logs = Enumerable.Range(1, count).Select(i => new LogHttp
            {
                ApplicationName = _options.ApplicationName,
                CreateDate = createDate,
                CreateDateOnly = DateOnly.FromDateTime(createDate),
                Path = $"/api/test/{i}",
                Method = "GET",
                HttpStatusCode = System.Net.HttpStatusCode.OK,
                Category = Reec.Inspection.ReecEnums.Category.OK
            });

            await _dbContext.LogHttps.AddRangeAsync(logs);
            await _dbContext.SaveChangesAsync();
        }

        private async Task SeedLogEndpointData(DateTime createDate, int count)
        {
            var logs = Enumerable.Range(1, count).Select(i => new LogEndpoint
            {
                ApplicationName = _options.ApplicationName,
                CreateDate = createDate,
                CreateDateOnly = DateOnly.FromDateTime(createDate),
                Path = $"/external/api/{i}",
                Method = "GET",
                HttpStatusCode = 200
            });

            await _dbContext.LogEndpoints.AddRangeAsync(logs);
            await _dbContext.SaveChangesAsync();
        }

        private async Task SeedLogJobData(DateTime createDate, int count)
        {
            var logs = Enumerable.Range(1, count).Select(i => new LogJob
            {
                ApplicationName = _options.ApplicationName,
                CreateDate = createDate,
                CreateDateOnly = DateOnly.FromDateTime(createDate),
                NameJob = $"OldJob{i}",
                StateJob = Reec.Inspection.ReecEnums.StateJob.Succeeded
            });

            await _dbContext.LogJobs.AddRangeAsync(logs);
            await _dbContext.SaveChangesAsync();
        }

        #endregion

        #region Helper Methods - Worker Process Methods

        private static async Task<string> CleanLogAuditWorker_Process(IServiceProvider service, ReecExceptionOptions option, IDateTimeService dateTime)
        {
            var dbContextService = service.GetRequiredService<IDbContextService>();
            var dbContext = dbContextService.GetDbContext();
            var day = DateOnly.FromDateTime(dateTime.Now.AddDays(-option.LogAudit.DeleteDays));
            var deletedTotal = 0;
            var count = 1;

            while (count > 0)
            {
                count = await dbContext.LogAudits.AsNoTracking()
                            .Where(w => w.CreateDateOnly <= day && w.ApplicationName == option.ApplicationName)
                            .Take(option.LogAudit.DeleteBatch)
                            .ExecuteDeleteAsync();
                deletedTotal += count;
            }

            return $"Limpieza completada: {deletedTotal} filas eliminadas";
        }

        private static async Task<string> CleanLogHttpWorker_Process(IServiceProvider service, ReecExceptionOptions option, IDateTimeService dateTime)
        {
            var dbContextService = service.GetRequiredService<IDbContextService>();
            var dbContext = dbContextService.GetDbContext();
            var day = DateOnly.FromDateTime(dateTime.Now.AddDays(-option.LogHttp.DeleteDays));
            var deletedTotal = 0;
            var count = 1;

            while (count > 0)
            {
                count = await dbContext.LogHttps.AsNoTracking()
                            .Where(w => w.CreateDateOnly <= day && w.ApplicationName == option.ApplicationName)
                            .Take(option.LogHttp.DeleteBatch)
                            .ExecuteDeleteAsync();
                deletedTotal += count;
            }

            return $"Limpieza completada: {deletedTotal} filas eliminadas";
        }

        private static async Task<string> CleanLogEndpointWorker_Process(IServiceProvider service, ReecExceptionOptions option, IDateTimeService dateTime)
        {
            var dbContextService = service.GetRequiredService<IDbContextService>();
            var dbContext = dbContextService.GetDbContext();
            var day = DateOnly.FromDateTime(dateTime.Now.AddDays(-option.LogEndpoint.DeleteDays));
            var deletedTotal = 0;
            var count = 1;

            while (count > 0)
            {
                count = await dbContext.LogEndpoints.AsNoTracking()
                            .Where(w => w.CreateDateOnly <= day && w.ApplicationName == option.ApplicationName)
                            .Take(option.LogEndpoint.DeleteBatch)
                            .ExecuteDeleteAsync();
                deletedTotal += count;
            }

            return $"Limpieza completada: {deletedTotal} filas eliminadas";
        }

        private static async Task<string> CleanLogJobWorker_Process(IServiceProvider service, ReecExceptionOptions option, IDateTimeService dateTime)
        {
            var dbContextService = service.GetRequiredService<IDbContextService>();
            var dbContext = dbContextService.GetDbContext();
            var day = DateOnly.FromDateTime(dateTime.Now.AddDays(-option.LogJob.DeleteDays));
            var deletedTotal = 0;
            var count = 1;

            while (count > 0)
            {
                count = await dbContext.LogJobs.AsNoTracking()
                            .Where(w => w.CreateDateOnly <= day && w.ApplicationName == option.ApplicationName)
                            .Take(option.LogJob.DeleteBatch)
                            .ExecuteDeleteAsync();
                deletedTotal += count;
            }

            return $"Limpieza completada: {deletedTotal} filas eliminadas";
        }

        #endregion

        public void Dispose()
        {
            TestDbContextFactory.CleanupContextWithServices(_dbContext, _serviceProvider);
        }
    }
}
