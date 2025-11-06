using Cronos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Reec.Inspection.Options;
using Reec.Inspection.Services;
using System;

namespace Reec.Inspection.Workers
{
    public class CleanAuditWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScope;
        private readonly ReecExceptionOptions _options;

        public CleanAuditWorker(IServiceScopeFactory serviceScope, ReecExceptionOptions options)
        {
            this._serviceScope = serviceScope;
            this._options = options;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //if (!_options.LogAudit.EnableClean)            
            var scope = _serviceScope.CreateAsyncScope();
            var provider = scope.ServiceProvider;
            var worker = provider.GetRequiredService<IWorker>();

            worker.NameJob = nameof(CleanAuditWorker);
            worker.RunFunction = (service) => Process(service, _options.LogAudit);
            worker.CreateUser = "Reec";
            worker.IsLightExecution = false;

            while (stoppingToken.IsCancellationRequested)
            {

                ////CronExpression expression = CronExpression.Parse(_options.LogAudit.CronValue);
                //CronExpression expression = CronExpression.Parse("1/1 * * * *"); //cada minuto.

                //DateTime? nextUtc = expression.GetNextOccurrence(DateTime.UtcNow);




                await worker.ExecuteAsync(default);

            }


        }

        private static async Task<string> Process(IServiceProvider service, LogAuditOption auditOption)
        {
            var dbContextService = service.GetRequiredService<IDbContextService>();
            var dbContext = dbContextService.GetDbContext();
            var day = DateOnly.FromDateTime(DateTime.Now.AddDays(-auditOption.DisposalDays));

            var count = 1;
            while (count > 0)
            {
                count = await dbContext.LogAudits.AsNoTracking()
                            .Where(w => w.CreateDateOnly <= day)
                            .Take(auditOption.DisposalBatch)
                            .ExecuteDeleteAsync();
            }
            return "Proceso Completado";
        }


    }
}
