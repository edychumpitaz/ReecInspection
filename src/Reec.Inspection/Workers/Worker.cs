using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Reec.Inspection.Entities;
using Reec.Inspection.Options;
using Reec.Inspection.Services;
using SecurityDriven;
using System.Collections;
using System.Diagnostics;
using static Reec.Inspection.ReecEnums;

namespace Reec.Inspection.Workers
{

    public sealed class Worker : IWorker
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly InspectionDbContext _dbContext;
        private readonly ReecExceptionOptions _reecOptions;
        private bool disposedValue;

        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider,
                            IHttpContextAccessor httpContextAccessor,
                            IDbContextService dbContextService,
                            ReecExceptionOptions reecOptions)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _dbContext = dbContextService.GetDbContext();
            this._reecOptions = reecOptions;
            if (httpContextAccessor != null && httpContextAccessor.HttpContext != null)
                TraceIdentifier = httpContextAccessor.HttpContext.TraceIdentifier;
            else
                TraceIdentifier = FastGuid.NewGuid().ToString();

            NameJob = "Anonymous";
        }

        public TimeSpan? Delay { get; set; } = null;
        public string NameJob { get; set; }
        public string TraceIdentifier { get; set; }
        public bool IsLightExecution { get; set; }
        public string CreateUser { get; set; }

        public Func<IServiceProvider, Task<string>> RunFunction { get; set; } = null;

        public Func<IServiceProvider, Exception, Task> RunFunctionException { get; set; } = null;

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            Exception innerException = null;
            Stopwatch stopwatch = new();

            try
            {
                try
                {
                    if (RunFunction == null)
                        throw new InvalidOperationException("No se ha definido ninguna función a ejecutar.");

                    _logger.LogInformation($"Tarea {NameJob} - Inicio de proceso en segundo plano.");
                    await CreateJob(StateJob.Enqueued, Delay, null, cancellationToken);

                    if (Delay.HasValue)
                    {
                        _logger.LogInformation($"Tarea {NameJob} - Se detuvo el proceso con un Delay de '{Delay.Value:hh\\:mm\\:ss}'");
                        await Task.Delay(Delay.Value, cancellationToken);
                        _logger.LogInformation($"Tarea {NameJob} - Se continua el proceso en segundo plano.");
                    }

                    await CreateJob(StateJob.Processing, null, null, cancellationToken);

                    // Lógica de la tarea programada
                    stopwatch.Start();
                    var message = await RunFunction.Invoke(_serviceProvider);

                    await CreateJob(StateJob.Succeeded, stopwatch.Elapsed, message, cancellationToken);
                    _logger.LogInformation($"Tarea {NameJob} - Fin del proceso en segundo plano.");
                }
                catch (Exception ex) when (RunFunctionException == null)
                {
                    innerException = ex;
                    await CreateException(stopwatch.Elapsed, ex, cancellationToken);
                }
                catch (Exception ex) when (RunFunctionException != null)
                {
                    innerException = ex;
                    var errorInit = $"Tarea {NameJob} - Se interceptó el error generado en su proceso de segundo plano.";
                    ex.Data.Add("ErrorInit", errorInit);
                    _logger.LogError(errorInit);

                    await CreateException(stopwatch.Elapsed, ex, cancellationToken);
                    await RunFunctionException.Invoke(_serviceProvider, ex);

                    var errorEnd = $"Tarea {NameJob} - Se finalizó el tratamiento de error de segundo plano.";
                    ex.Data.Add("ErrorEnd", errorEnd);
                    _logger.LogError(errorEnd);
                }
            }
            catch (Exception ex)
            {
                var customException = new AggregateException("Ocurrió uno o más errores.", innerException, ex);
                await CreateException(stopwatch.Elapsed, customException, cancellationToken);
            }
            finally
            {
                stopwatch.Stop();
            }

        }

        private async Task CreateJob(StateJob enumJob, TimeSpan? duration, string message, CancellationToken cancellationToken)
        {
            if (!IsLightExecution)
            {
                var job = new LogJob
                {
                    ApplicationName = _reecOptions.ApplicationName,
                    NameJob = NameJob,
                    StateJob = enumJob,
                    CreateDateOnly = DateOnly.FromDateTime(DateTime.Now),
                    CreateDate = DateTime.Now,
                    CreateUser = CreateUser,
                    Duration = duration,
                    TraceIdentifier = TraceIdentifier,
                    Message = message
                };
                await _dbContext.LogJobs.AddAsync(job, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _dbContext.DetachEntity(job);
            }
            _logger.LogInformation($"Tarea {NameJob} - {enumJob}");

        }

        private async Task CreateException(TimeSpan? duration, Exception ex, CancellationToken cancellationToken)
        {
            var failed = new LogJob
            {
                ApplicationName = _reecOptions.ApplicationName,
                NameJob = NameJob,
                StateJob = StateJob.Failed,
                CreateDateOnly = DateOnly.FromDateTime(DateTime.Now),
                CreateDate = DateTime.Now,
                CreateUser = CreateUser,
                Exception = ex.Message,
                StackTrace = ex.ToString(),
                Duration = duration,
                TraceIdentifier = TraceIdentifier
            };
            if (ex.InnerException != null)
                failed.InnerException = ex.InnerException.Message;

            if (ex.Data.Count > 0)
                failed.Data = ConvertToDictionary(ex.Data);

            await _dbContext.LogJobs.AddAsync(failed, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogError(ex, $"Tarea {NameJob} - {StateJob.Failed}");
            _dbContext.DetachEntity(failed);
        }

        private static Dictionary<string, object> ConvertToDictionary(IDictionary data)
        {
            var dictionary = new Dictionary<string, object>();
            foreach (DictionaryEntry entry in data)
                if (entry.Key is string key)
                    dictionary[key] = entry.Value;

            return dictionary;
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _dbContext.Dispose();
                    Delay = null;
                    NameJob = null;
                    TraceIdentifier = null;
                    CreateUser = null;
                    RunFunction = null;
                    RunFunctionException = null;
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            //GC.SuppressFinalize(this);
        }
    }

}
