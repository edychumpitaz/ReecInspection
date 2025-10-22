using System.Net;
using static Reec.Inspection.ReecEnums;

namespace Reec.Inspection.Entities
{
    public class LogHttp
    {

        public int IdLogHttp { get; set; }

        /// <summary>
        /// Nombre del aplicativo que registra el error.
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Tipo de mensaje que retornará al usuario.
        /// </summary>
        public Category Category { get; set; }

        public string CategoryDescription { get; set; }

        /// <summary>
        /// Contiene los valores de los códigos de estado definidos para HTTP. 
        /// </summary>
        public HttpStatusCode HttpStatusCode { get; set; }

        /// <summary>
        /// Mensaje que se le envía al cliente
        /// </summary>
        public string MessageUser { get; set; }

        /// <summary>
        /// Tiempo transcurrido.
        /// </summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// Obtiene o establece un identificador único para representar esta solicitud en los registros de seguimiento. 
        /// <para>Se obtiene desde las cabeceras del request, este valor suele venir por el marcado de un ApiGateway o Balanceador.</para>
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// Mensaje de Error producido por una excepción.
        /// </summary>
        public string ExceptionMessage { get; set; }

        /// <summary>
        /// Mensaje de Error producido por una excepción que a su vez tiene un InnerException.
        /// </summary>
        public string InnerExceptionMessage { get; set; }

        /// <summary>
        /// Protocolo de conexión.
        /// </summary>
        public string Protocol { get; set; }

        public bool IsHttps { get; set; }

        /// <summary>
        /// Metodo de solicitud HTTP: GET, POST, PUT, DELETE, PATCH, ETC
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Esquema de solicitud HTTP.
        /// </summary>
        public string Scheme { get; set; }

        /// <summary>
        /// Nombre del servidor.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Puerto del servidor donde se aloja el aplicativo.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Nombre y puerto del aplicativo: 
        /// <para>Ejemplo: localhost:53174</para>
        /// </summary>
        public string HostPort { get; set; }

        /// <summary>
        /// Ruta del aplicativo: api/controller/action
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Parametros de consulta que se encuentra en la URL de la petición Request.
        /// </summary>
        public string QueryString { get; set; }

        /// <summary>
        /// Origen de la excepción.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Obtiene o establece un identificador único para representar esta solicitud en los registros de seguimiento.
        /// </summary>
        public string TraceIdentifier { get; set; }

        /// <summary>
        /// ContentType del HttpRequest.
        /// </summary>
        public string ContentType { get; set; }

        ///// <summary>
        ///// Nombre de la aplicacion cliente desde donde se conecta al servicio.
        ///// Se obtiene desde la cabecera del Request
        ///// </summary>
        //public string RequestClient { get; set; } 

        /// <summary>
        /// Datos enviados por el cliente en el HEADER de la solicitud HTTP.
        /// </summary>
        public Dictionary<string, string> RequestHeader { get; set; }

        /// <summary>
        /// Datos enviados por el cliente en el BODY de la solicitud HTTP.
        /// </summary>
        public string RequestBody { get; set; }

        /// <summary>
        /// Pila de llamadas que origino el error.
        /// </summary>
        public string StackTrace { get; set; }

        /// <summary>
        /// Dirección IP del cliente que envia el request.
        /// </summary>
        public string IpAddress { get; set; }

        public DateOnly? CreateDateOnly { get; set; }

        /// <summary>
        /// Obtiene el usuario autenticado desde un Json Web Token(jwt).
        /// </summary>
        public string CreateUser { get; set; }
        public DateTime? CreateDate { get; set; }

    }
}
