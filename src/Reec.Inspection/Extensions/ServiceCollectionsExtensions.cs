﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

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
        public static IServiceCollection AddReecException<TDbContext>(
                                            this IServiceCollection services,
                                            [NotNull] Action<DbContextOptionsBuilder> action,
                                            ReecExceptionOptions exceptionOptions = null)
                                        where TDbContext : InspectionDbContext
        {
            var options = exceptionOptions ?? new ReecExceptionOptions();
            services.AddTransient(opt => options);
            services.AddTransient<ReecExceptionMiddleware<TDbContext>>();
            services.AddDbContext<TDbContext>(action, ServiceLifetime.Transient, ServiceLifetime.Transient);
            services.AddHostedService<ReecWorker<TDbContext>>();
            if (options.EnableProblemDetails)
                services.AddProblemDetails();

            //if (options.EnableMigrations)
            //{
            //    using var scope = services.BuildServiceProvider().CreateScope();
            //    //_logger.LogInformation($"Reec: Inicio de la migración de tabla de log '{_options.TableName}'.");
            //    using var context = scope.ServiceProvider.GetRequiredService<TDbContext>();
            //     context.Database.Migrate();
            //    //_logger.LogInformation($"Reec: Fin de la migración de tabla de log '{_options.TableName}'.");
            //}

            return services;
        }
    }

}
