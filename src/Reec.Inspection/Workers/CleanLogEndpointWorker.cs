using Cronos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Reec.Inspection.Options;
using Reec.Inspection.Services;

namespace Reec.Inspection.Workers
{
    public class CleanLogEndpointWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScope;
        private readonly ReecExceptionOptions _options;
        private readonly IDateTimeService _dateTime;

        public CleanLogEndpointWorker(IServiceScopeFactory serviceScope, ReecExceptionOptions options, IDateTimeService dateTime)
        {
            this._serviceScope = serviceScope;
            this._options = options;
            this._dateTime = dateTime;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.LogAudit.EnableClean)
                return; 

            var scope = _serviceScope.CreateAsyncScope();
            var provider = scope.ServiceProvider;
            var worker = provider.GetRequiredService<IWorker>();

            worker.NameJob = nameof(CleanLogEndpointWorker);
            worker.RunFunction = (service) => Process(service, _options);
            worker.CreateUser = "Reec";
            worker.IsLightExecution = false;

            while (!stoppingToken.IsCancellationRequested)
            {
                CronExpression expression = CronExpression.Parse(_options.LogEndpoint.CronValue);
                var nextUtc = expression.GetNextOccurrence(_dateTime.UtcNow, _dateTime.TimeZoneInfo);
                if (nextUtc.HasValue)
                {
                    var timeLocal = TimeZoneInfo.ConvertTime(nextUtc.Value, _dateTime.TimeZoneInfo);
                    var delay = timeLocal - _dateTime.Now;
                    if (delay.TotalMilliseconds > 0)
                        await Task.Delay(delay, stoppingToken);
                }
                await worker.ExecuteAsync(default);
            }
        }

        private static async Task<string> Process(IServiceProvider service, ReecExceptionOptions option)
        {
            var dbContextService = service.GetRequiredService<IDbContextService>();
            var dbContext = dbContextService.GetDbContext();
            var day = DateOnly.FromDateTime(DateTime.Now.AddDays(-option.LogEndpoint.DeleteDays));
            var count = 1;
            while (count > 0)
            {
                count = await dbContext.LogEndpoints.AsNoTracking()
                            .Where(w => w.CreateDateOnly <= day && w.ApplicationName == option.ApplicationName)
                            .Take(option.LogAudit.DeleteBatch)
                            .ExecuteDeleteAsync();
            }
            return "Proceso Completado.";
        }

    }
}
