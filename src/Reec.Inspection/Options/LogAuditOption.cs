namespace Reec.Inspection.Options
{

    /// <summary>
    /// Representa la configuración general utilizada por el middleware de auditoría <b>Reec.Inspection</b>.
    /// </summary>
    /// <remarks>
    /// Esta clase define los parámetros que controlan el comportamiento del registro de auditoría
    /// para las solicitudes y respuestas HTTP procesadas por el servidor.
    ///
    /// <para>
    /// Las opciones permiten configurar la persistencia en base de datos, límites de tamaño para cuerpos de mensaje,
    /// rutas excluidas, y parámetros relacionados con el rendimiento y almacenamiento temporal.
    /// </para>
    ///
    /// <para>
    /// El middleware utiliza estas opciones para interceptar y registrar información de trazabilidad,
    /// incluyendo cuerpo de petición, cuerpo de respuesta, tiempos de ejecución y metadatos de contexto.
    /// </para>
    /// </remarks>m
    public class LogAuditOption
    {
        /// <summary>
        /// Indica si los registros de auditoría deben persistirse en la base de datos.
        /// </summary>
        /// <remarks>
        /// Si se establece en <see langword="false"/>, el middleware continuará procesando la auditoría
        /// pero los datos no serán almacenados de forma persistente.
        /// </remarks>
        public bool IsSaveDB { get; set; } = true;

        /// <summary>
        /// Nombre del esquema de base de datos donde se almacenarán los registros de auditoría.
        /// </summary>
        /// <remarks>
        /// Este valor permite definir el esquema personalizado que contendrá la tabla de auditoría.
        /// Si no se especifica, se utilizará el esquema por defecto configurado en la conexión.
        ///
        /// <para>
        /// Esta configuración aplica cuando las tablas de logs ya existen en la base de datos 
        /// y no se están generando mediante migraciones automáticas (<see cref="ReecExceptionOptions.EnableMigrations"/> = <see langword="false"/>).
        /// </para>
        ///
        /// <para>
        /// Ejemplo: <c>options.LogAudit.Schema = "Inspection";</c>
        /// </para>
        /// </remarks>
        public string Schema { get; set; } = null;

        /// <summary>
        /// Nombre de la tabla de base de datos destinada a almacenar los registros de auditoría.
        /// </summary>
        /// <remarks>
        /// Valor por defecto: <c>"LogAudit"</c>.
        /// Se recomienda mantener un nombre representativo del propósito de auditoría o trazabilidad.
        ///
        /// <para>
        /// Esta configuración debe utilizarse cuando las tablas ya están creadas en la base de datos
        /// y el sistema no ejecuta migraciones automáticas (<see cref="ReecExceptionOptions.EnableMigrations"/> = <see langword="false"/>).
        /// </para>
        ///
        /// <para>
        /// Ejemplo: <c>options.LogAudit.TableName = "LogAuditoria";</c>
        /// </para>
        /// </remarks>
        public string TableName { get; set; } = "LogAudit";


        /// <summary>
        /// Tamaño máximo permitido, en bytes, para registrar el cuerpo de la respuesta HTTP.
        /// </summary>
        /// <remarks>
        /// Si el cuerpo de la respuesta supera este límite, no será almacenado para evitar sobrecarga de datos.
        /// <para>Valor por defecto: <c>32 * 1024</c> (32 KB).</para>
        /// </remarks>
        public int ResponseBodyMaxSize { get; set; } = (32 * 1024);

        /// <summary>
        /// Tamaño máximo permitido, en bytes, para registrar el cuerpo de la solicitud HTTP.
        /// </summary>
        /// <remarks>
        /// Si el cuerpo de la solicitud excede este tamaño, no se registrará su contenido para optimizar el rendimiento del middleware.
        /// <para>Valor por defecto: <c>32 * 1024</c> (32 KB).</para>
        /// </remarks>
        public int RequestBodyMaxSize { get; set; } = (32 * 1024);

        /// <summary>
        /// Colección de rutas o segmentos que deben excluirse del proceso de auditoría.
        /// </summary>
        /// <remarks>
        /// Útil para omitir endpoints estáticos o de documentación como Swagger o favicon.
        /// <para>Valor por defecto: <c>["swagger", "index", "favicon"]</c>.</para>
        /// </remarks>
        public IList<string> ExcludePaths { get; set; } = ["swagger", "index", "favicon"];

        /// <summary>
        /// Habilita el almacenamiento en búfer del flujo de solicitud para permitir la lectura del cuerpo del request múltiples veces.
        /// </summary>
        /// <remarks>
        /// Requerido para registrar el contenido del body sin interferir con el flujo original de la petición.
        /// <para>Valor por defecto: <see langword="true"/>.</para>
        /// </remarks>
        public bool EnableBuffering { get; set; } = true;


        /// <summary>
        /// Indica si el proceso en segundo plano encargado de limpiar los registros de log en la base de datos está habilitado.
        /// </summary>
        /// <remarks>
        /// Cuando está en <see langword="true"/>, el servicio ejecutará la tarea de eliminación según la expresión cron configurada.
        /// </remarks>
        public bool EnableClean { get; set; } = true;

        /// <summary>
        /// Expresión CRON que define la programación de la tarea de limpieza.
        /// </summary>
        /// <remarks>
        /// Por defecto, se ejecuta todos los días a las <b>2:00 a.m.</b>.
        /// <para>Formato estándar: <c>0 2 * * *</c></para>
        /// Asegúrate de que la expresión sea válida y compatible con el programador utilizado (por ejemplo <b>Cronos</b>).
        /// 
        /// <para>
        /// Consulta y valida tus expresiones en: 
        /// <see href="https://crontab.guru/">https://crontab.guru</see>
        /// </para>
        /// </remarks>
        public string CronValue { get; set; } = "0 2 * * *";

        /// <summary>
        /// Número de días hacia atrás, desde la fecha actual, cuyos registros serán eliminados.
        /// </summary>
        /// <remarks>
        /// Por ejemplo, si el valor es <c>10</c>, se eliminarán los registros con más de 10 días de antigüedad.
        /// </remarks>
        public int DeleteDays { get; set; } = 10;

        /// <summary>
        /// Cantidad máxima de registros eliminados por lote durante la ejecución de la tarea.
        /// </summary>
        /// <remarks>
        /// Este valor permite controlar el volumen de eliminación en cada iteración, optimizando el rendimiento y evitando sobrecarga en la base de datos.
        /// </remarks>
        public int DeleteBatch { get; set; } = 100;

    }
}
