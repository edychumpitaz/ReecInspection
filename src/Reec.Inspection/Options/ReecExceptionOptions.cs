using static Reec.Inspection.ReecEnums;

namespace Reec.Inspection.Options
{
    /// <summary>
    /// Configuraciones de Reec.
    /// <para>La próxima versión se va a migrar por defecto la respuesta del objeto a ProblemDetails</para>
    /// </summary>
    public class ReecExceptionOptions
    {
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
        /// Habilita la migración automática a la Base de Datos.
        /// <para>Valor por defecto: true.</para> 
        /// </summary>
        public bool EnableMigrations { get; set; } = true;

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

        /// <summary>
        /// Configuración de tabla de peticiones HTTP de auditoría, captura que ingresó y que salió del sistema.
        /// </summary>
        public LogAuditOption LogAudit { get; set; } = new();

        /// <summary>
        /// Configurarión de tabla errores de StoreProcedure en la base de datos.
        /// </summary>
        public LogDbOption LogDb { get; set; } = new();

        /// <summary>
        /// Configuración de tabla de endpoints externos del sistema.
        /// <para>Captura que salió y que ingresó al consultar servicios endpoint fuera del sistema.</para>  
        /// <para>Ejemplo: Llamadas a servicios REST externos.</para>
        /// </summary>
        public LogEndpointOption LogEndpoint { get; set; } = new();

        /// <summary>
        /// Configuración de tabla de peticiones HTTP para captura de errores optiene toda la pila de llamadas.
        /// </summary>
        public LogHttpOption LogHttp { get; set; } = new();

        /// <summary>
        /// Configuración de tabla de trabajos en segundo plano (Background Jobs).
        /// </summary>
        public LogJobOption LogJob { get; set; } = new();        

    }
}