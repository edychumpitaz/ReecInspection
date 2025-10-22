using Microsoft.AspNetCore.Builder;

namespace Reec.Inspection.Extensions
{
    public static class ApplicationBuilderExtensions
    {

        /// <summary>
        /// Middleware encargado de interceptar el HttpRequest y de capturar los errores generados para guardar en base de datos.
        /// </summary>
        /// <typeparam name="TDbContext"></typeparam>
        /// <param name="applicationBuilder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseReecException<TDbContext>(this IApplicationBuilder applicationBuilder) where TDbContext : InspectionDbContext
        {
            applicationBuilder.UseMiddleware<ReecExceptionMiddleware<TDbContext>>();
            return applicationBuilder;
        }


    }




}
