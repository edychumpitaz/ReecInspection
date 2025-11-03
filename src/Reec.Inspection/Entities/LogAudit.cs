using System.Net;

namespace Reec.Inspection.Entities
{
    public class LogAudit
    {
        public int IdLogAudit { get; set; }

        /// <summary>
        /// Nombre del aplicativo que registra el error.
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Contiene los valores de los códigos de estado definidos para HTTP. 
        /// </summary>
        public HttpStatusCode HttpStatusCode { get; set; }

        /// <summary>
        /// Obtiene o establece un identificador único para representar esta solicitud en los registros de seguimiento. 
        /// <para>Se obtiene desde las cabeceras del request, este valor suele venir por el marcado de un ApiGateway o Balanceador.</para>
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// Tiempo transcurrido.
        /// </summary>
        public TimeSpan? Duration { get; set; }

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
        /// Obtiene o establece un identificador único para representar esta solicitud en los registros de seguimiento.
        /// </summary>
        public string TraceIdentifier { get; set; }

        /// <summary>
        /// Datos enviados por el cliente en el HEADER de la solicitud HTTP.
        /// </summary>
        public Dictionary<string, string> RequestHeader { get; set; }

        /// <summary>
        /// Datos enviados por el cliente en el BODY de la solicitud HTTP.
        /// </summary>
        public string RequestBody { get; set; }

        /// <summary>
        /// Datos enviados por el servidor en el HEADER de la solicitud HTTP.
        /// </summary>
        public Dictionary<string, string> ResponseHeader { get; set; }

        /// <summary> 
        /// Datos enviados por el servidor en el BODY de la solicitud HTTP.
        /// </summary>
        public string ResponseBody { get; set; }

        /// <summary>
        /// Dirección IP del cliente que envia el request.
        /// </summary>
        public string IpAddress { get; set; }

        public DateOnly? CreateDateOnly { get; set; }
        public string CreateUser { get; set; }
        public DateTime CreateDate { get; set; }


    }
}
