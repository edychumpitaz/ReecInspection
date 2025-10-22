namespace Reec.Inspection
{
    public class ReecEnums
    {

        /// <summary>
        /// Categoria de los mensajes enviados al cliente.
        /// </summary>
        public enum Category
        {

            /// <summary>
            /// Mensaje Informativo. El HttpStatus se marca como 200.
            /// <para>Ejemplo: Se utiliza para los datos guardados, consulta exitosa. </para>
            /// </summary>
            OK = 200,

            /// <summary>
            /// Mensaje Informativo. El HttpStatus se marca como 206.
            /// <para>Ejemplo: Se utiliza cuando la consulta es satisfactoria pero no contiene información.</para>
            /// </summary>
            PartialContent = 206,

            /// <summary>
            /// Validación de Permisos: Se requiere autenticación. El HttpStatus se marca como 401.
            /// <para>Ejemplo: Se utiliza cuando se requiere logear el usuario.</para>
            /// </summary>
            Unauthorized = 401,

            /// <summary>
            /// Validación de Permisos: Se requiere autorización para acceder a un recurso. El HttpStatus se marca como 403.
            /// <para>Ejemplo: Se utiliza cuando el usuario logeado no cumple con los permisos o roles necesarios.</para>
            /// </summary>
            Forbidden = 403,



            /// <summary>
            /// Validación de campos: El HttpStatus se marca como 400.
            /// <para>Ejemplo: Se utiliza para validar si los datos guardados cumplen con los requisitos.</para>
            /// </summary>
            Warning = 460,

            /// <summary>
            /// Validación de Business Logic: El HttpStatus se marca como 400.
            /// <para>Ejemplo: Se utiliza cuando exísta ERRORES CONTROLADOS por el sistema.</para>
            /// </summary>
            BusinessLogic = 465,

            /// <summary>
            /// Validación de Business Logic: El HttpStatus se marca como 400.
            /// <para>Ejemplo: Se utiliza cuando exíste ERRORES CONTROLADOS de un SISTEMA EXISTENTE(Base de Datos, Servicio Api, etc).</para>
            /// </summary>
            BusinessLogicLegacy = 470,



            /// <summary>
            /// Error interno del servidor: El HttpStatus se marca como 500.
            /// <para>Ejemplo: Se utiliza cuando exíste ERRORES NO CONTROLADOS por el sistema.</para>
            /// </summary>
            InternalServerError = 500,

            /// <summary>
            /// Mensaje de Error de un sistema existente(api, wcf, soap). El HttpStatus se marca como 502.
            /// <para>Ejemplo: Se utiliza cuando exíste ERRORES NO CONTROLADOS de un SISTEMA EXISTENTE.</para>
            /// </summary>
            BadGateway = 502,

            /// <summary>
            /// Tiempo de espera agotado. El HttpStatus se marca como 504.
            /// <para>Ejemplo: Se utiliza cuando se conecta a un SISTEMA EXISTENTE(api, wcf, soap) y supera el TIEMPO DE ESPERA.</para>
            /// </summary>
            GatewayTimeout = 504

        }


    }

}
