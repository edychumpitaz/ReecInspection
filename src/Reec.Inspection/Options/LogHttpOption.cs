namespace Reec.Inspection.Options
{

    /// <summary>
    /// Representa la configuración principal del módulo <b>Reec.Inspection</b>, 
    /// responsable de capturar y registrar errores HTTP de forma automática sin requerir bloques <c>try/catch</c> en el código.
    /// </summary>
    /// <remarks>
    /// Esta clase define las opciones de comportamiento del middleware encargado de interceptar
    /// las solicitudes HTTP entrantes, registrar los errores ocurridos en el pipeline y
    /// almacenar la información relevante en la base de datos.
    ///
    /// <para>
    /// Su propósito es simplificar la gestión de excepciones, permitiendo una trazabilidad centralizada
    /// de los errores generados por la aplicación sin intervención manual del desarrollador.
    /// </para>
    ///
    /// <para>
    /// Los registros generados por este componente incluyen información del contexto HTTP:
    /// cabeceras, cuerpo, dirección IP, identificadores de trazabilidad y metadatos del error.
    /// </para>
    /// </remarks>
    public class LogHttpOption
    {
        /// <summary>
        /// Indica si los registros HTTP deben persistirse en la base de datos.
        /// </summary>
        /// <remarks>
        /// Si se establece en <see langword="false"/>, el middleware seguirá interceptando las excepciones
        /// pero no realizará almacenamiento persistente (útil para entornos de desarrollo o prueba).
        /// </remarks>
        public bool IsSaveDB { get; set; } = true;

        /// <summary>
        /// Nombre del esquema de base de datos donde se almacenarán los registros de errores HTTP.
        /// </summary>
        /// <remarks>
        /// Si no se especifica, se utiliza el esquema por defecto de la conexión configurada.
        /// </remarks>
        public string Schema { get; set; } = null;

        /// <summary>
        /// Nombre de la tabla en base de datos utilizada para almacenar los registros de errores HTTP.
        /// </summary>
        /// <remarks>
        /// Valor por defecto: <c>"LogHttp"</c>.
        /// </remarks>
        public string TableName { get; set; } = "LogHttp";


        /// <summary>
        /// Lista de claves de encabezado (<c>Header</c>) del <see cref="HttpRequest"/> que deben excluirse del proceso de inspección antes de su almacenamiento.
        /// </summary>
        /// <remarks>
        /// Útil para omitir información sensible o repetitiva como <c>Authorization</c> o <c>Cookie</c>.
        /// </remarks>
        public List<string> HeaderKeysExclude { get; set; } = null;

        /// <summary>
        /// Lista de claves de encabezado (<c>Header</c>) del <see cref="HttpRequest"/> que se deben incluir explícitamente en la inspección.
        /// </summary>
        /// <remarks>
        /// Si se especifica, únicamente las cabeceras listadas serán registradas.
        /// <para>
        /// Ejemplo: si el request contiene 10 cabeceras y <see cref="HeaderKeysInclude"/> tiene 2 elementos,
        /// solo esas dos claves serán almacenadas en la base de datos.
        /// </para>
        /// </remarks>
        public List<string> HeaderKeysInclude { get; set; } = null;

        /// <summary>
        /// Nombre de la cabecera que contiene la dirección IP del cliente.
        /// </summary>
        /// <remarks>
        /// Se utiliza para extraer la dirección IP cuando la aplicación está detrás de un proxy, 
        /// balanceador o API Gateway (por ejemplo: <c>X-Forwarded-For</c>).
        /// </remarks>
        public string IpAddressFromHeader { get; set; }

        /// <summary>
        /// Nombre de la cabecera que contiene el identificador de correlación o trazabilidad de la petición.
        /// </summary>
        /// <remarks>
        /// Puede corresponder a cabeceras comunes como <c>TraceIdentifier</c>, <c>CorrelationId</c> o <c>X-Correlation-ID</c>.
        /// Si no se define, se utiliza el <see cref="HttpContext.TraceIdentifier"/> como valor predeterminado.
        /// </remarks>
        public string RequestIdFromHeader { get; set; }


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
