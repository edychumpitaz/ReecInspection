using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Reec.Inspection.Middlewares;
using Reec.Inspection.Options;

namespace Reec.Inspection.Extensions
{
    public static class ApplicationBuilderExtensions
    {

        /// <summary>
        /// Middleware encargado de interceptar el HttpRequest y de capturar los errores generados para guardar en base de datos.     
        /// <typeparam name="TDbContext"></typeparam>
        /// <param name="applicationBuilder"></param>
        /// <returns></returns>
        [Obsolete("UseReecException está obsoleto. Utilice UseReecInspection en su lugar.")]
        public static IApplicationBuilder UseReecException<TDbContext>(this IApplicationBuilder applicationBuilder) where TDbContext : InspectionDbContext
        {
            var option = applicationBuilder.ApplicationServices.GetRequiredService<ReecExceptionOptions>();
            if (option.EnableGlobalDbSave)
            {
                if (option.LogAudit.IsSaveDB)
                    applicationBuilder.UseMiddleware<LogAuditMiddleware>();

                if (option.LogHttp.IsSaveDB)
                    applicationBuilder.UseMiddleware<LogHttpMiddleware>();
            }
            return applicationBuilder;
        }


        /// <summary>
        /// Middleware encargado de interceptar el HttpRequest y de capturar los errores generados para guardar en base de datos.
        /// </summary>
        /// <param name="applicationBuilder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseReecInspection(this IApplicationBuilder applicationBuilder)
        {
            var option = applicationBuilder.ApplicationServices.GetRequiredService<ReecExceptionOptions>();
            if (option.EnableGlobalDbSave)
            {
                if (option.LogAudit.IsSaveDB)
                    applicationBuilder.UseMiddleware<LogAuditMiddleware>();

                if (option.LogHttp.IsSaveDB)
                    applicationBuilder.UseMiddleware<LogHttpMiddleware>();

            }
            return applicationBuilder;
        }

    }


}
