namespace Reec.Inspection.Options
{
    public class LogDbOption
    {
        /// <summary>
        /// Nombre del esquema de Base de Datos.
        /// </summary>
        public string Schema { get; set; } = null;

        /// <summary>
        /// Nombre de la tabla de Base de datos. 
        /// <para>Valor por defecto: "LogDb".</para>
        /// </summary>
        public string TableName { get; set; } = "LogDb";
    }
}

