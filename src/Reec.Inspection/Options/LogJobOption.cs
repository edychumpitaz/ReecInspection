namespace Reec.Inspection.Options
{
    public class LogJobOption
    {
        /// <summary>
        /// Nombre del esquema de Base de Datos.
        /// </summary>
        public string Schema { get; set; } = null;

        /// <summary>
        /// Nombre de la tabla de Base de datos. 
        /// <para>Valor por defecto: "LogJob".</para>
        /// </summary>
        public string TableName { get; set; } = "LogJob";
    }
}
