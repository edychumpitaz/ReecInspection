using static Reec.Inspection.ReecEnums;

namespace Reec.Inspection
{
    /// <summary>
    /// Configuraciones de Reec.
    /// <para>La próxima versión se va a migrar por defecto la respuesta del objeto a ProblemDetails</para>
    /// </summary>
    public class ReecExceptionOptions
    {
        /// <summary>
        /// Lista de claves de cabecera HttpRequest que se debe excluir de la inspección antes de guardar a la Base de Datos. 
        /// </summary>
        public List<string> HeaderKeysExclude { get; set; } = null;

        /// <summary>
        /// Lista de claves de cabecera HttpRequest que solo se debe incluir de la inspección. 
        /// <para>Ejemplo: Si la cabecera trae 10 keys y la variable HeaderKeysInclude tiene 2 elementos, pues solo 2 key se guardan en BD.</para>
        /// </summary>
        public List<string> HeaderKeysInclude { get; set; } = null;

        /// <summary>
        /// Nombre de la aplicación que registra la excepción.
        /// </summary>
        public string ApplicationName { get; set; } = null;

        /// <summary>
        /// Mensaje de error del sistema. 
        /// <para>Valor por defecto: "Error no controlado del sistema."</para>
        /// </summary>
        public string InternalServerErrorMessage { get; set; } = "Error no controlado del sistema.";

        /// <summary>
        /// Mensaje de error al no poder guardar información en la Base de Datos.
        /// <para>Valor por defecto: "Ocurrió un error al guardar log en Base de Datos."</para>
        /// </summary>
        public string ApplicationErrorMessage { get; set; } = "Ocurrió un error al guardar log en Base de Datos.";

        /// <summary>
        /// Nombre del esquema de Base de Datos.
        /// </summary>
        public string Schema { get; set; } = null;

        /// <summary>
        /// Nombre de la tabla de Base de datos. 
        /// <para>Valor por defecto: "LogHttp".</para>
        /// </summary>
        public string TableName { get; set; } = "LogHttp";

        /// <summary>
        /// Habilita la migración automática a la Base de Datos.
        /// <para>Valor por defecto: true.</para> 
        /// </summary>
        public bool EnableMigrations { get; set; } = true;

        /// <summary>
        /// Dirección IP que se puede tomar desde los Headers del request.
        /// <para>Ingresar el nombre de alguna variable del Header</para>
        /// </summary>
        public string IpAddressFromHeader { get; set; }

        /// <summary>
        /// Identifica el ID de la petición del request, suele ser llamado "TraceIdentifier, CorrelationId, Correlation-x".
        /// <para>Se utiliza un valor de la cabecera del Request que proviene desde un ApiGateway o Balanceador.</para>
        /// <para>Valor por defecto: Toma el TraceIdentifier del request</para>
        /// </summary> 
        public string RequestIdFromHeader { get; set; }

        /// <summary>
        /// Habilitar almacenamiento en búfer. Se utiliza para poder leer el contenido del body de la petición request.
        /// <para>Valor por defecto: true.</para>
        /// </summary>
        public bool EnableBuffering { get; set; } = true;

        /// <summary>
        /// Habilita la respuesta del ReecException con el objeto estandar de ProblemDetails.
        /// <para>Valor por defecto: false.</para>
        /// </summary>
        public bool EnableProblemDetails { get; set; } = false;

        /// <summary>
        /// Categoria mínima que Reec va almacenar en base de datos.
        /// <para>valor por defecto: Category.Unauthorized(401)</para>
        /// </summary>
        public Category MinCategory { get; set; } = Category.Unauthorized;
    }

}
