using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Reec.Inspection
{
    public class ReecWorker<TDbContext> : IHostedService
                       where TDbContext : InspectionDbContext
    {
        private readonly IServiceScopeFactory _serviceScope;
        private readonly ILogger<ReecWorker<TDbContext>> _logger;
        private readonly ReecExceptionOptions _options;       

        public ReecWorker(IServiceScopeFactory serviceScope, ILogger<ReecWorker<TDbContext>> logger,
                            ReecExceptionOptions options
            )
        {
            this._serviceScope = serviceScope;
            this._logger = logger;
            this._options = options;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_options.EnableMigrations)
            {
                using var scope = _serviceScope.CreateScope();
                _logger.LogInformation($"Reec: Inicio de la migración de tabla de log '{_options.TableName}'.");
                using var context = scope.ServiceProvider.GetRequiredService<TDbContext>();
                await context.Database.MigrateAsync(cancellationToken);
                _logger.LogInformation($"Reec: Fin de la migración de tabla de log '{_options.TableName}'.");
            }            
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Reec: Se detuvo correctamente proceso de migración.");
            return Task.CompletedTask;
        }

    }
}
