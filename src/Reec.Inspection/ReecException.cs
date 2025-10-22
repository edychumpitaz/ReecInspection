using static Reec.Inspection.ReecEnums;

namespace Reec.Inspection
{
    public class ReecException : Exception
    {

        public ReecMessage ReecMessage { get; set; }

        /// <summary>
        /// Excepción capturada por el desarrollador cuando necesita colocar un try catch
        /// </summary>
        public string ExceptionMessage { get => this.Data[nameof(ExceptionMessage)]?.ToString(); }

        /// <summary>
        /// Se utiliza para mensajes simples.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="messageUser">Mensaje que retornara al cliente</param>
        public ReecException(Category category, string messageUser)
            : base(messageUser)
        {
            this.ReecMessage = new ReecMessage(category, messageUser);
        }

        /// <summary>
        /// Se utiliza para mensajes simples.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="messageUser">Lista de mensajes que retornara al cliente</param>
        public ReecException(Category category, List<string> messageUser)
            : base(string.Join("|", messageUser))
        {
            this.ReecMessage = new ReecMessage(category, messageUser.ToList());
        }

        /// <summary>
        /// Captura de error personalizada con ExceptionMessage de origen.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="messageUser">Mensaje que retornará al cliente.</param>
        /// <param name="exceptionMessage">Mensaje producido por una excepcion controlada.</param>
        public ReecException(Category category, string messageUser, string exceptionMessage)
            : base(exceptionMessage)
        {
            this.ReecMessage = new ReecMessage(category, messageUser);
            this.Data.Add(nameof(ExceptionMessage), exceptionMessage);
        }

        /// <summary>
        /// Captura de error avanzada con ExceptionMessage de origen e InnerException.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="messageUser">Mensaje que retornará al cliente.</param>
        /// <param name="exceptionMessage">Mensaje producido por una excepcion controlada.</param>
        /// <param name="innerException">Mensaje producido por una excepcion controlada y contiene un InnerException.</param>
        public ReecException(Category category, string messageUser, string exceptionMessage, Exception innerException)
            : base(exceptionMessage, innerException)
        {
            this.ReecMessage = new ReecMessage(category, messageUser);
            this.Data.Add(nameof(ExceptionMessage), exceptionMessage);
            //this.Data.Add("InnerException", innerException);
        }


    }

}
