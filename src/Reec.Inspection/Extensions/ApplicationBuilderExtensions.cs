using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Reec.Inspection.Middlewares;
using Reec.Inspection.Options;

namespace Reec.Inspection.Extensions
{
    public static class ApplicationBuilderExtensions
    {

        /// <summary>
        /// (Obsoleto) Agrega al pipeline los middlewares de Reec.Inspection para auditoría de requests y captura automática de errores.
        /// </summary>
        /// <typeparam name="TDbContext">
        /// Tipo de <c>InspectionDbContext</c> mantenido por compatibilidad binaria con versiones anteriores.
        /// No es utilizado en tiempo de ejecución.
        /// </typeparam>
        /// <param name="applicationBuilder">Builder del pipeline HTTP.</param>
        /// <returns>El mismo <see cref="IApplicationBuilder"/> para encadenamiento.</returns>
        /// <remarks>
        /// Equivale a <see cref="UseReecInspection(IApplicationBuilder)"/>. 
        /// Se mantiene solo para proyectos que aún llaman al método genérico de versiones previas.
        /// <para>
        /// Comportamiento:
        /// <list type="bullet">
        /// <item>
        /// <description>
        /// Si <see cref="ReecExceptionOptions.EnableGlobalDbSave"/> es <see langword="true"/>,
        /// registra condicionalmente:
        /// <list type="bullet">
        ///   <item><description><c>LogAuditMiddleware</c> si <c>LogAudit.IsSaveDB</c> es <see langword="true"/>: intercepta solicitud/respuesta (auditoría).</description></item>
        ///   <item><description><c>LogHttpMiddleware</c> si <c>LogHttp.IsSaveDB</c> es <see langword="true"/>: captura excepciones y contexto HTTP.</description></item>
        /// </list>
        /// </description>
        /// </item>
        /// </list>
        /// </para>
        /// <para>
        /// Orden recomendado en el pipeline (ejemplo típico):
        /// <code>
        /// app.UseRouting();
        /// app.UseAuthentication();
        /// app.UseAuthorization();
        /// app.UseReecInspection(); // &lt;= aquí
        /// app.MapControllers();
        /// </code>
        /// </para>
        /// </remarks>
        [Obsolete("UseReecException está obsoleto. Utilice UseReecInspection en su lugar.")]
        public static IApplicationBuilder UseReecException<TDbContext>(this IApplicationBuilder applicationBuilder)
                                                        where TDbContext : InspectionDbContext
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
        /// Agrega al pipeline los middlewares de Reec.Inspection para auditoría de requests y captura automática de errores.
        /// </summary>
        /// <param name="applicationBuilder">Builder del pipeline HTTP.</param>
        /// <returns>El mismo <see cref="IApplicationBuilder"/> para encadenamiento.</returns>
        /// <remarks>
        /// Registra condicionalmente los middlewares de auditoría/errores según <see cref="ReecExceptionOptions"/>:
        /// <list type="bullet">
        /// <item>
        /// <description>
        /// Requiere <see cref="ReecExceptionOptions.EnableGlobalDbSave"/> = <see langword="true"/> para habilitar la persistencia global.
        /// </description>
        /// </item>
        /// <item><description><c>LogAuditMiddleware</c> si <c>LogAudit.IsSaveDB</c> es <see langword="true"/>: registra método, ruta, headers (según filtros), cuerpo (respetando límites), estado y tiempos.</description></item>
        /// <item><description><c>LogHttpMiddleware</c> si <c>LogHttp.IsSaveDB</c> es <see langword="true"/>: captura excepciones del pipeline, guarda stack trace y metadatos, y puede responder en <c>ProblemDetails</c> si está habilitado.</description></item>
        /// </list>
        ///
        /// <para>
        /// Orden recomendado en el pipeline (guía):
        /// <code>
        /// app.UseRouting();
        /// app.UseAuthentication();
        /// app.UseAuthorization();
        /// app.UseReecInspection(); // intercepta antes de los endpoints
        /// app.MapControllers();
        /// </code>
        /// </para>
        ///
        /// <para>
        /// Prerrequisitos habituales:
        /// <list type="bullet">
        ///   <item><description>Registrar opciones y DBContext en <c>Program.cs</c>:
        ///   <code>
        /// builder.Services.AddReecInspection&lt;InspectionDbContext&gt;(
        ///     db =&gt; db.UseSqlServer(connString),
        ///     options =&gt;
        ///     {
        ///         options.EnableGlobalDbSave = true;
        ///         options.LogAudit.IsSaveDB = true;
        ///         options.LogHttp.IsSaveDB  = true;
        ///     });
        ///   </code>
        ///   </description></item>
        ///   <item><description>Si se desea capturar cuerpos grandes, considerar habilitar el buffering en la request.</description></item>
        /// </list>
        /// </para>
        ///
        /// <para>
        /// Nota: Los módulos <c>LogEndpoint</c> y <c>LogJob</c> no son middlewares HTTP;
        /// se gestionan desde servicios y workers (salientes y background), respectivamente.
        /// </para>
        /// </remarks>
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
