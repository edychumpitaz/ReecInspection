using static Reec.Inspection.ReecEnums;

namespace Reec.Inspection.Options
{
    /// <summary>
    /// Representa la configuración general del componente <b>Reec.Inspection</b>,
    /// encargado de la captura, auditoría y manejo centralizado de excepciones en aplicaciones .NET.
    /// </summary>
    /// <remarks>
    /// Esta clase agrupa todas las opciones de configuración utilizadas por los distintos módulos de <b>Reec</b>,
    /// permitiendo definir el comportamiento global del sistema de observabilidad, registro y trazabilidad.
    ///
    /// <para>
    /// Incluye configuraciones para auditorías de solicitudes entrantes (<see cref="LogAudit"/>),
    /// llamadas a servicios externos (<see cref="LogEndpoint"/>),
    /// manejo automático de excepciones HTTP (<see cref="LogHttp"/>),
    /// y monitoreo de trabajos en segundo plano (<see cref="LogJob"/>).
    /// </para>
    ///
    /// <para>
    /// Además, permite definir mensajes de error predeterminados, comportamiento de migraciones automáticas,
    /// nivel mínimo de severidad a registrar y compatibilidad opcional con el formato estándar
    /// <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/>.
    /// </para>
    ///
    /// <example>
    /// Ejemplo de configuración en <c>Program.cs</c>:
    /// <code>
    /// builder.Services.AddReecInspection&lt;DbContext&gt;(
    ///     db =&gt; db.UseSqlServer("cadena de conexión"),
    ///     options =&gt;
    ///     {
    ///         options.ApplicationName = "Reec.Inspeccion.Api";
    ///         options.EnableMigrations = false;
    ///         options.EnableProblemDetails = true;
    ///         options.MinCategory = Category.Unauthorized;
    ///
    ///         options.LogHttp.TableName = "LogHttp";
    ///         options.LogAudit.TableName = "LogAudit";
    ///         options.LogJob.TableName = "LogJob";
    ///         options.LogEndpoint.Schema = "Integration";
    ///     });
    /// </code>
    /// </example>
    ///
    /// <para>
    /// En futuras versiones, <b>Reec</b> adoptará de forma predeterminada la salida de errores basada en
    /// <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> para mejorar la estandarización de respuestas.
    /// </para>
    /// </remarks>

    public class ReecExceptionOptions
    {
        /// <summary>
        /// Nombre de la aplicación que registra las excepciones y logs de auditoría.
        /// </summary>
        public string ApplicationName { get; set; } = null;

        /// <summary>
        /// Mensaje genérico utilizado para errores internos del sistema.
        /// </summary>
        /// <remarks>
        /// Valor por defecto: <c>"Error no controlado del sistema."</c>.
        /// </remarks>
        public string InternalServerErrorMessage { get; set; } = "Error no controlado del sistema.";

        /// <summary>
        /// Mensaje mostrado cuando ocurre un error al intentar guardar información en la base de datos.
        /// </summary>
        /// <remarks>
        /// Valor por defecto: <c>"Ocurrió un error al guardar log en Base de Datos."</c>.
        /// </remarks>
        public string ApplicationErrorMessage { get; set; } = "Ocurrió un error al guardar log en Base de Datos.";

        /// <summary>
        /// Habilita la ejecución de migraciones automáticas para la creación o actualización de tablas de logs.
        /// </summary>
        /// <remarks>
        /// Valor por defecto: <see langword="true"/>.
        /// </remarks>
        public bool EnableMigrations { get; set; } = true;

        /// <summary>
        /// Habilita la respuesta de errores en formato <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/>.
        /// </summary>
        /// <remarks>
        /// Valor por defecto: <see langword="false"/>.
        /// </remarks>
        public bool EnableProblemDetails { get; set; } = false;

        /// <summary>
        /// Indica si se debe habilitar el guardado global de logs en base de datos para observabilidad ligera.
        /// </summary>
        public bool EnableGlobalDbSave { get; set; } = true;

        /// <summary>
        /// Define la categoría mínima de error que será almacenada en base de datos.
        /// </summary>
        /// <remarks>
        /// Valor por defecto: <see cref="Category.Unauthorized"/> (HTTP 401).
        /// </remarks>
        public Category MinCategory { get; set; } = Category.Unauthorized;

        /// <summary>
        /// Configuración del módulo de auditoría de solicitudes HTTP entrantes.
        /// </summary>
        /// <remarks>
        /// Captura los datos que ingresan y salen del sistema.
        /// </remarks>
        public LogAuditOption LogAudit { get; set; } = new();

        /// <summary>
        /// Configuración del módulo de registro de endpoints externos.
        /// </summary>
        /// <remarks>
        /// Captura las solicitudes y respuestas enviadas a servicios externos como Sunat, Reniec o SAP.
        /// </remarks>
        public LogEndpointOption LogEndpoint { get; set; } = new();

        /// <summary>
        /// Configuración del módulo de registro y trazabilidad de errores HTTP.
        /// </summary>
        /// <remarks>
        /// Permite la captura automática de excepciones sin necesidad de bloques <c>try/catch</c>,
        /// registrando el contexto completo del error.
        /// </remarks>
        public LogHttpOption LogHttp { get; set; } = new();

        /// <summary>
        /// Configuración del módulo de registro de tareas y procesos en segundo plano.
        /// </summary>
        /// <remarks>
        /// Captura información de ejecución, errores y métricas de background jobs.
        /// </remarks>
        public LogJobOption LogJob { get; set; } = new();

    }
}