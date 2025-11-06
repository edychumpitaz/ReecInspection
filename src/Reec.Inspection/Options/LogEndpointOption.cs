namespace Reec.Inspection.Options
{

    /// <summary>
    /// Representa la configuración de auditoría para las peticiones salientes del sistema hacia servicios externos.
    /// </summary>
    /// <remarks>
    /// Esta clase define los parámetros utilizados por el middleware de <b>Reec.Inspection</b> encargado de interceptar 
    /// y registrar la comunicación con sistemas externos (por ejemplo: Sunat, Reniec, SAP, entre otros).
    ///
    /// <para>
    /// Permite configurar el esquema y nombre de la tabla donde se almacenan los registros de interacción, 
    /// así como habilitar o deshabilitar la persistencia de los datos.
    /// </para>
    ///
    /// <para>
    /// Los registros generados por este módulo suelen incluir información como:
    /// URL de destino, método HTTP, encabezados, cuerpo de la solicitud y respuesta, 
    /// tiempos de ejecución, códigos de estado y excepciones si las hubiera.
    /// </para>
    /// </remarks>
    public class LogEndpointOption
    {
        /// <summary>
        /// Nombre del esquema de base de datos donde se almacenarán los registros de endpoints externos.
        /// </summary>
        /// <remarks>
        /// Si no se especifica, se usará el esquema por defecto configurado en la conexión de base de datos.
        ///
        /// <para>
        /// Esta configuración aplica cuando las tablas de logs ya existen en la base de datos
        /// y no se están creando mediante migraciones automáticas 
        /// (<see cref="ReecExceptionOptions.EnableMigrations"/> = <see langword="false"/>).
        /// </para>
        ///
        /// <para>
        /// Ejemplo: <c>options.LogEndpoint.Schema = "Inspection";</c>
        /// </para>
        /// </remarks>
        public string Schema { get; set; } = null;

        /// <summary>
        /// Nombre de la tabla de base de datos destinada a registrar las peticiones y respuestas hacia servicios externos.
        /// </summary>
        /// <remarks>
        /// Valor por defecto: <c>"LogEndpoint"</c>.
        ///
        /// <para>
        /// Se debe configurar cuando las tablas ya se encuentran creadas manualmente en la base de datos 
        /// y el sistema no utiliza migraciones automáticas 
        /// (<see cref="ReecExceptionOptions.EnableMigrations"/> = <see langword="false"/>).
        /// </para>
        ///
        /// <para>
        /// Ejemplo: <c>options.LogEndpoint.TableName = "LogEndpointExterno";</c>
        /// </para>
        /// </remarks>
        public string TableName { get; set; } = "LogEndpoint";


        /// <summary>
        /// Indica si los registros de comunicación con servicios externos deben persistirse en la base de datos.
        /// </summary>
        /// <remarks>
        /// Si se establece en <see langword="false"/>, el Handler del Http podrá continuar trazando las llamadas 
        /// sin realizar almacenamiento persistente.
        /// </remarks>
        public bool IsSaveDB { get; set; } = true;


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
