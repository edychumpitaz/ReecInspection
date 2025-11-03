using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Reec.Inspection.HttpMessageHandler;
using Reec.Inspection.Middlewares;
using Reec.Inspection.Options;
using Reec.Inspection.Services;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;

namespace Reec.Inspection.Extensions
{
    public static class ServiceCollectionsExtensions
    {

        /// <summary>
        /// Agregamos servicio de control de errores automáticos Reec.
        /// <para>La proxima versión se va a migrar por defecto la respuesta del objeto ProblemDetails</para>
        /// </summary>
        /// <param name="services"></param>
        /// <param name="action">Agregamos configuración de base de datos</param>
        /// <param name="exceptionOptions">Opciones de filtros personalizados.</param>
        /// <returns></returns>
        [Obsolete(message: "La próxima versión 10 se va a elimiar la extensión, usar AddReecInspection")]
        public static IServiceCollection AddReecException<TDbContext>(
                                            this IServiceCollection services,
                                            [NotNull] Action<DbContextOptionsBuilder> action,
                                            ReecExceptionOptions exceptionOptions = null)
                                        where TDbContext : InspectionDbContext
        {
            var options = exceptionOptions ?? new ReecExceptionOptions();
            services.AddTransient(serviceProvider => options); 

            services.AddDbContext<TDbContext>(action, ServiceLifetime.Transient, ServiceLifetime.Transient);

            services.AddTransient<LogHttpMiddleware<TDbContext>>();
            services.AddTransient<LogEndpointHandler>();
            services.AddScoped<IDbContextService, DbContextService<TDbContext>>();
            services.AddHostedService<ReecWorker<TDbContext>>();

            if (options.EnableProblemDetails)
                services.AddProblemDetails();

            return services;
        }

        /// <summary>
        /// Agregamos servicio de control de errores automáticos Reec.
        /// <para>La proxima versión se va a migrar por defecto la respuesta del objeto ProblemDetails</para>
        /// </summary>
        /// <param name="services"></param>
        /// <param name="action">Agregamos configuración de base de datos</param>
        /// <param name="Options">Opciones de filtros personalizados.</param>
        /// <param name="poolSize"></param>
        /// <returns></returns>
        public static IServiceCollection AddReecInspection<TDbContext>(
                                            this IServiceCollection services,
                                            [NotNull] Action<DbContextOptionsBuilder> action,
                                            Action<ReecExceptionOptions> Options,
                                            int poolSize = 1024)
                                        where TDbContext : InspectionDbContext
        {
            var options = new ReecExceptionOptions();
            Options.Invoke(options); 
            services.AddTransient(serviceProvider => options);
            //services.AddDbContext<TDbContext>(action, ServiceLifetime.Transient, ServiceLifetime.Transient);
            services.AddDbContextPool<TDbContext>(action, poolSize);

            services.AddTransient<LogHttpMiddleware<TDbContext>>();
            services.AddTransient<LogEndpointHandler>();
            services.AddScoped<IDbContextService, DbContextService<TDbContext>>();
            services.AddHostedService<ReecWorker<TDbContext>>();

            if (options.EnableProblemDetails)
                services.AddProblemDetails();

            return services;
        }

        /// <summary>
        /// Se registra la observabilidad de los endpoint externos para ser guardados en base de datos.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddBaseResilienceObservability(this IServiceCollection services, IHttpClientBuilder httpClientBuilder)
        {

            //services.TryAddTransient<LogEndpointHandler>();
            httpClientBuilder.AddHttpMessageHandler<LogEndpointHandler>();

            httpClientBuilder.AddStandardResilienceHandler()
                .Configure((options, serviceProvider) =>
                {
                    options.Retry.BackoffType = DelayBackoffType.Exponential;
                    options.Retry.ShouldHandle = args =>
                    {
                        bool isException = false;
                        if (args.Outcome.Exception is not null)
                            isException = args.Outcome.Exception is SocketException or HttpRequestException or TimeoutException or TaskCanceledException;
                        else
                            isException = !args.Outcome.Result.IsSuccessStatusCode;

                        var request = args.Context.GetRequestMessage();
                        if (request is not null)
                        {
                            HttpRequestOptionsKey<int> RetryKey = new("RetryAttempts");
                            request.Options.Set(RetryKey, args.AttemptNumber);
                        }

                        return ValueTask.FromResult(isException);
                    };

                    var requestMessageKey = new ResiliencePropertyKey<HttpRequestMessage>("Resilience.Http.RequestMessage");

                    // (si quieres también marcar CB abierto, timeout, etc.)
                    options.CircuitBreaker.OnOpened = args =>
                    {
                        if (args.Context.Properties.TryGetValue(requestMessageKey, out var request))
                        {
                            HttpRequestOptionsKey<bool> circuit = new("CircuitOpened");
                            request.Options.Set(circuit, true);
                        }
                        return default;
                    };
                });

            return services;
        }


    }

}
