using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Reec.Inspection.HttpMessageHandler;
using Reec.Inspection.Middlewares;
using Reec.Inspection.Options;
using Reec.Inspection.Services;
using Reec.Inspection.Workers;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;

namespace Reec.Inspection.Extensions
{
    public static class ServiceCollectionsExtensions
    {

        /// <summary>
        /// (Obsoleto) Registra los servicios principales de <b>Reec.Inspection</b> para la captura automática de errores y auditoría.
        /// </summary>
        /// <typeparam name="TDbContext">
        /// Tipo del contexto de base de datos que hereda de <see cref="InspectionDbContext"/>.
        /// </typeparam>
        /// <param name="services">Contenedor de servicios de la aplicación (<see cref="IServiceCollection"/>).</param>
        /// <param name="action">Configuración del <see cref="DbContextOptionsBuilder"/> para inicializar el contexto de datos.</param>
        /// <param name="exceptionOptions">Instancia personalizada de <see cref="ReecExceptionOptions"/>. Si no se proporciona, se crea una por defecto.</param>
        /// <returns>La colección de servicios actualizada para encadenamiento.</returns>
        /// <remarks>
        /// Esta extensión está marcada como obsoleta y será eliminada en la versión 9.  
        /// Utilice <see cref="AddReecInspection{TDbContext}(IServiceCollection, Action{DbContextOptionsBuilder}, Action{ReecExceptionOptions}, int)"/> en su lugar.
        ///
        /// <example>
        /// Ejemplo de uso:
        /// <code>
        /// builder.Services.AddReecException&lt;InspectionDbContext&gt;(
        ///     db =&gt; db.UseSqlServer(connString),
        ///     new ReecExceptionOptions { EnableGlobalDbSave = true });
        /// </code>
        /// </example>
        /// </remarks>
        [Obsolete("La próxima versión 9 eliminará esta extensión. Use AddReecInspection.")]
        public static IServiceCollection AddReecException<TDbContext>(
                                            this IServiceCollection services,
                                            [NotNull] Action<DbContextOptionsBuilder> action,
                                            ReecExceptionOptions exceptionOptions = null)
                                        where TDbContext : InspectionDbContext
        {
            var options = exceptionOptions ?? new ReecExceptionOptions();
            services.AddSingleton(options);
            services.AddDbContext<TDbContext>(action, ServiceLifetime.Transient, ServiceLifetime.Transient);

            services.AddTransient<LogEndpointHandler>();
            services.AddTransient<IWorker, Worker>();
            services.AddScoped<IDbContextService, DbContextService<TDbContext>>();
            services.AddScoped<LogAuditMiddleware>();
            services.AddScoped<LogHttpMiddleware>();
            services.AddHostedService<ReecWorker<TDbContext>>();
            services.AddHostedService<CleanLogAuditWorker>();
            services.AddHostedService<CleanLogEndpointWorker>();
            services.AddHostedService<CleanLogHttpWorker>();
            services.AddHostedService<CleanLogJobWorker>();
            services.AddSingleton<IDateTimeService, DateTimeService>();

            if (options.EnableProblemDetails)
                services.AddProblemDetails();
            services.AddHttpContextAccessor();
            services.Configure<HostOptions>(options =>
            {
                options.ServicesStartConcurrently = true; // Los HostedService arrancan en paralelo
                options.ServicesStopConcurrently = true;  // Se detienen en paralelo                
            });

            return services;
        }


        /// <summary>
        /// Registra los servicios de <b>Reec.Inspection</b> para auditoría de requests, captura automática de errores
        /// y limpieza periódica de tablas de log mediante workers en segundo plano.
        /// </summary>
        /// <typeparam name="TDbContext">
        /// Contexto que hereda de <see cref="InspectionDbContext"/> y gestiona las tablas de logs.
        /// </typeparam>
        /// <param name="services">Contenedor de servicios (<see cref="IServiceCollection"/>).</param>
        /// <param name="action">Configuración del <see cref="DbContextOptionsBuilder"/> para <typeparamref name="TDbContext"/>.</param>
        /// <param name="Options">
        /// Delegado para configurar <see cref="ReecExceptionOptions"/> (p. ej., <c>SystemTimeZoneId</c>, <c>EnableProblemDetails</c>,
        /// y las opciones de cada módulo: <c>LogAudit</c>, <c>LogEndpoint</c>, <c>LogHttp</c>, <c>LogJob</c>).
        /// </param>
        /// <param name="poolSize">Tamaño del pool de DbContext. Por defecto: <c>1024</c>.</param>
        /// <returns><see cref="IServiceCollection"/> para encadenamiento.</returns>
        /// <remarks>
        /// Registra:
        /// <list type="bullet">
        ///   <item><description>Middlewares: <c>LogAuditMiddleware</c> y <c>LogHttpMiddleware</c>.</description></item>
        ///   <item><description>Ejecución segura de jobs: <see cref="IWorker"/> / <see cref="Worker"/>.</description></item>
        ///   <item><description>Fecha/hora regional: <see cref="IDateTimeService"/>.</description></item>
        ///   <item><description>Workers de limpieza (condicionales):
        ///     <list type="bullet">
        ///       <item><description><c>CleanLogAuditWorker</c> — limpia <c>LogAudit</c> si <c>LogAudit.EnableClean</c> es true.</description></item>
        ///       <item><description><c>CleanLogEndpointWorker</c> — limpia <c>LogEndpoint</c> si <c>LogEndpoint.EnableClean</c> es true.</description></item>
        ///       <item><description><c>CleanLogHttpWorker</c> — limpia <c>LogHttp</c> si <c>LogHttp.EnableClean</c> es true.</description></item>
        ///       <item><description><c>CleanLogJobWorker</c> — limpia <c>LogJob</c> si <c>LogJob.EnableClean</c> es true.</description></item>
        ///     </list>
        ///     Cada worker respeta <c>CronValue</c>, <c>DisposalDays</c>, <c>DisposalBatch</c> y <c>SystemTimeZoneId</c>.
        ///   </description></item>
        /// </list>
        /// Si <see cref="ReecExceptionOptions.EnableProblemDetails"/> es <see langword="true"/>, se agrega <c>AddProblemDetails()</c>.
        /// </remarks>
        public static IServiceCollection AddReecInspection<TDbContext>(
                                            this IServiceCollection services,
                                            [NotNull] Action<DbContextOptionsBuilder> action,
                                            Action<ReecExceptionOptions> Options,
                                            int poolSize = 1024)
                                        where TDbContext : InspectionDbContext
        {
            var options = new ReecExceptionOptions();
            Options.Invoke(options);
            services.AddSingleton(options);
            services.AddDbContextPool<TDbContext>(action, poolSize);

            services.AddTransient<LogEndpointHandler>();
            services.AddTransient<IWorker, Worker>();
            services.AddScoped<IDbContextService, DbContextService<TDbContext>>();
            services.AddScoped<LogAuditMiddleware>();
            services.AddScoped<LogHttpMiddleware>();
            services.AddHostedService<ReecWorker<TDbContext>>();
            services.AddHostedService<CleanLogAuditWorker>();
            services.AddHostedService<CleanLogEndpointWorker>();
            services.AddHostedService<CleanLogHttpWorker>();
            services.AddHostedService<CleanLogJobWorker>();
            services.AddSingleton<IDateTimeService, DateTimeService>();

            if (options.EnableProblemDetails)
                services.AddProblemDetails();
            services.AddHttpContextAccessor();
            services.Configure<HostOptions>(options =>
            {
                options.ServicesStartConcurrently = true; // Los HostedService arrancan en paralelo
                options.ServicesStopConcurrently = true;  // Se detienen en paralelo                
            });

            return services;
        }


        /// <summary>
        /// Configura la observabilidad y resiliencia para los endpoints HTTP externos, integrando
        /// <b>Reec.Inspection</b> con <see cref="HttpClient"/> y el pipeline estándar de resiliencia (retry/timeout/circuit breaker).
        /// </summary>
        /// <param name="services">Contenedor de servicios de la aplicación (<see cref="IServiceCollection"/>).</param>
        /// <param name="httpClientBuilder">
        /// Instancia de <see cref="IHttpClientBuilder"/> a la cual se aplicarán el <c>LogEndpointHandler</c>
        /// y las políticas de resiliencia.
        /// </param>
        /// <param name="timeout">
        /// Tiempo máximo total permitido para una solicitud HTTP. Si es <see langword="null"/>, por defecto es <c>1 minuto</c>.
        /// </param>
        /// <returns>
        /// Un <see cref="IHttpStandardResiliencePipelineBuilder"/> para continuar configurando el pipeline si se requiere.
        /// </returns>
        /// <remarks>
        /// Registra:
        /// <list type="bullet">
        ///   <item><description><c>LogEndpointHandler</c> para registrar Request/Response, códigos HTTP y errores.</description></item>
        ///   <item><description>Pipeline estándar con timeout total, reintentos exponenciales y circuit breaker.</description></item>
        /// </list>
        ///
        /// <example>
        /// Registro con cliente nombrado (ejemplo real):
        /// <code>
        /// var httpBuilder = builder.Services.AddHttpClient("PlaceHolder", httpClient =&gt;
        /// {
        ///     httpClient.DefaultRequestHeaders.Clear();
        ///     httpClient.BaseAddress = new Uri("https://jsonplaceholder.typicode.com");
        /// });
        /// builder.Services.AddReecInspectionResilience(httpBuilder);
        ///
        /// // Consumo:
        /// var factory = app.Services.GetRequiredService&lt;IHttpClientFactory&gt;();
        /// var client  = factory.CreateClient("PlaceHolder");
        /// var resp    = await client.GetAsync("/todos/1");
        /// resp.EnsureSuccessStatusCode();
        /// </code>
        ///
        /// Registro sobreescribiendo el timeout total (opcional):
        /// <code>
        /// var httpBuilder = builder.Services.AddHttpClient("PlaceHolder", c =&gt;
        /// {
        ///     c.BaseAddress = new Uri("https://jsonplaceholder.typicode.com");
        /// });
        /// builder.Services.AddReecInspectionResilience(httpBuilder, TimeSpan.FromSeconds(30));
        /// </code>
        /// </example>
        /// </remarks>
        public static IHttpStandardResiliencePipelineBuilder AddReecInspectionResilience(this IServiceCollection services,
                                    IHttpClientBuilder httpClientBuilder, TimeSpan? timeout = null)
        {

            //services.TryAddTransient<LogEndpointHandler>();
            httpClientBuilder.AddHttpMessageHandler<LogEndpointHandler>();

            var pipeline = httpClientBuilder.AddStandardResilienceHandler()
                            .Configure((options, serviceProvider) =>
                            {
                                if (timeout is null)
                                    options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(1);
                                else
                                    options.TotalRequestTimeout.Timeout = timeout.Value;

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

            return pipeline;
        }


    }

}
