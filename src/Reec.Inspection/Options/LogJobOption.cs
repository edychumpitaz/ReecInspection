namespace Reec.Inspection.Options
{

    /// <summary>
    /// Representa la configuración de auditoría y persistencia para los procesos en segundo plano o tareas programadas.
    /// </summary>
    /// <remarks>
    /// Esta clase define los parámetros utilizados por el módulo <b>Reec.Inspection</b> para registrar la ejecución
    /// de <see cref="IHostedService"/>, <see cref="BackgroundService"/>.
    ///
    /// <para>
    /// Su propósito es capturar y almacenar información sobre la ejecución de tareas automáticas, incluyendo
    /// fecha de inicio, duración, estado final, errores ocurridos y datos de contexto.
    /// </para>
    ///
    /// <para>
    /// Al igual que los demás módulos de la librería, incorpora mecanismos de limpieza automática mediante
    /// expresiones CRON y eliminación por lotes, lo que permite gestionar el crecimiento de los registros en base de datos.
    /// </para>
    /// </remarks>
    public class LogJobOption
    {
        /// <summary>
        /// Indica si los registros de ejecución de tareas deben persistirse en la base de datos.
        /// </summary>
        /// <remarks>
        /// Si se establece en <see langword="false"/>, el sistema seguirá capturando la información en memoria
        /// sin realizar almacenamiento persistente.
        /// </remarks>
        public bool IsSaveDB { get; set; } = true;

        /// <summary>
        /// Nombre del esquema de base de datos donde se almacenarán los registros de ejecución de tareas en segundo plano.
        /// </summary>
        /// <remarks>
        /// Si no se especifica, se usará el esquema por defecto configurado en la conexión de base de datos.
        ///
        /// <para>
        /// Esta configuración aplica cuando las tablas de logs ya existen en la base de datos 
        /// y no se están generando mediante migraciones automáticas 
        /// (<see cref="ReecExceptionOptions.EnableMigrations"/> = <see langword="false"/>).
        /// </para>
        ///
        /// <para>
        /// Ejemplo: <c>options.LogJob.Schema = "Inspection";</c>
        /// </para>
        /// </remarks>
        public string Schema { get; set; } = null;

        /// <summary>
        /// Nombre de la tabla de base de datos destinada a registrar la ejecución de procesos automáticos o background jobs.
        /// </summary>
        /// <remarks>
        /// Valor por defecto: <c>"LogJob"</c>.
        ///
        /// <para>
        /// Se debe configurar cuando las tablas ya se encuentran creadas manualmente en la base de datos 
        /// y el sistema no utiliza migraciones automáticas 
        /// (<see cref="ReecExceptionOptions.EnableMigrations"/> = <see langword="false"/>).
        /// </para>
        ///
        /// <para>
        /// Ejemplo: <c>options.LogJob.TableName = "LogJobProcess";</c>
        /// </para>
        /// </remarks>
        public string TableName { get; set; } = "LogJob";


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
        /// <see href="https://crontab.guru">https://crontab.guru</see>
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
