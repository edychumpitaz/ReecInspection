namespace Reec.Inspection.Options
{
    public class LogHttpOption
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
        /// Nombre del esquema de Base de Datos.
        /// </summary>
        public string Schema { get; set; } = null;

        /// <summary>
        /// Nombre de la tabla de Base de datos. 
        /// <para>Valor por defecto: "LogHttp".</para>
        /// </summary>
        public string TableName { get; set; } = "LogHttp";
              
    }
}
