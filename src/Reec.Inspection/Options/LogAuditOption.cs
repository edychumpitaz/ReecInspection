namespace Reec.Inspection.Options
{
    public class LogAuditOption
    {
        /// <summary>
        /// Nombre del esquema de Base de Datos.
        /// </summary>
        public string Schema { get; set; } = null;

        /// <summary>
        /// Nombre de la tabla de Base de datos. 
        /// <para>Valor por defecto: "LogAudit".</para>
        /// </summary>
        public string TableName { get; set; } = "LogAudit";

        /// <summary>
        /// Valor expresado en en bytes que indica el tamaño máximo del cuerpo de la solicitud.
        /// <para>Si el cuerpo de la respuesta excede este tamaño, no se registrará.</para>
        /// <para>Valor por defecto: 32 * 1024 = (32Kb).</para>
        /// </summary>
        public int ResponseBodyMaxSize { get; set; } = (32 * 1024);

        /// <summary>
        /// Valor expresado en en bytes que indica el tamaño máximo del cuerpo de la solicitud.
        /// <para>Si el cuerpo de la solicitud excede este tamaño, no se registrará.</para>
        /// <para>Valor por defecto: 32 * 1024 = (32Kb).</para>
        /// </summary>
        public int RequestBodyMaxSize { get; set; } = (32 * 1024);

        /// <summary>
        /// Lista de rutas a excluir del registro de auditoría.
        /// <para>Valor por defecto: ["swagger", "index", "favicon"].</para>
        /// </summary>
        public IList<string> ExcludePaths { get; set; } = ["swagger", "index", "favicon"];

        /// <summary>
        /// Indica si se debe guardar el log de auditoría en la base de datos.
        /// </summary>
        public bool IsSaveDB { get; set; } = true;

    }
}
