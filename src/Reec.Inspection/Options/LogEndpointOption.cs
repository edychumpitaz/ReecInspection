namespace Reec.Inspection.Options
{
    public class LogEndpointOption
    {
        /// <summary>
        /// Nombre del esquema de Base de Datos.
        /// </summary>
        public string Schema { get; set; } = null;

        /// <summary>
        /// Nombre de la tabla de Base de datos. 
        /// <para>Valor por defecto: "LogEndpoint".</para>
        /// </summary>
        public string TableName { get; set; } = "LogEndpoint";
    }
}
