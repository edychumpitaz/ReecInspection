using Cronos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Reec.Inspection.Options;
using Reec.Inspection.Services;

namespace Reec.Inspection.Workers
{
    public class CleanLogJobWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScope;
        private readonly ReecExceptionOptions _options;
        private readonly IDateTimeService _dateTime;

        public CleanLogJobWorker(IServiceScopeFactory serviceScope, ReecExceptionOptions options, IDateTimeService dateTime)
        {
            this._serviceScope = serviceScope;
            this._options = options;
            this._dateTime = dateTime;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.LogJob.EnableClean)
                return;

            while (!stoppingToken.IsCancellationRequested)
            {
                CronExpression expression = CronExpression.Parse(_options.LogJob.CronValue);
                var nextUtc = expression.GetNextOccurrence(_dateTime.UtcNow, _dateTime.TimeZoneInfo);
                if (nextUtc.HasValue)
                {
                    var timeLocal = TimeZoneInfo.ConvertTime(nextUtc.Value, _dateTime.TimeZoneInfo);
                    var delay = timeLocal - _dateTime.Now;
                    if (delay.TotalMilliseconds > 0)
                        await Task.Delay(delay, stoppingToken);
                }
                else
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

                using var scope = _serviceScope.CreateScope();
                var provider = scope.ServiceProvider;
                var worker = provider.GetRequiredService<IWorker>();

                worker.NameJob = nameof(CleanLogJobWorker);
                worker.RunFunction = (service) => Process(service, _options, _dateTime, stoppingToken);
                worker.CreateUser = "Reec";
                worker.IsLightExecution = false;
                await worker.ExecuteAsync(stoppingToken);
            }
        }

        private static async Task<string> Process(IServiceProvider service, ReecExceptionOptions option, IDateTimeService dateTime, CancellationToken cancellationToken)
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
                            .ExecuteDeleteAsync(cancellationToken);
                deletedTotal += count;
            }
            return $"Proceso Completado: {nameof(CleanLogJobWorker)} limpió {deletedTotal} filas (corte: {day})";
        }

    }
}
