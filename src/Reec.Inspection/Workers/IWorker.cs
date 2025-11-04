using Microsoft.AspNetCore.Http;
using static Reec.Inspection.ReecEnums;
namespace Reec.Inspection.Workers
{
    /// <summary> 
    /// Interfaz personalizada para ejecutar proceso en segundo plano en modo seguro.
    /// <para>La interfaz proporciona una función <see cref = "RunFunction()" ></see> donde ejecuta el proceso en segundo plano.</para>
    /// <para>La interfaz proporciona una función <see cref = "RunFunctionException()"></see> para la captura de la excepción generada.</para> 
    /// </summary>
    public interface IWorker : IDisposable
    {
        /// <summary>
        /// Tiempo de espera antes de ejecutar el proceso en segundo plano.
        /// </summary>
        TimeSpan? Delay { get; set; }

        /// <summary>
        /// Nombre de tarea en segundo plano. Valor por defecto <c>'Anonymous'</c>.
        /// </summary>
        string NameJob { get; set; }

        /// <summary>
        /// Trace de identificación para procesos en segundo plano.
        /// <para>Valor por defecto: Cuando el proceso en segundo plano nace desde un <c>'request'</c> se obtiene el valor <see cref = "HttpContext.TraceIdentifier"></see>.</para> 
        /// <para>Caso contrario se obtiene el valor de  <see cref = "FastGuid.NewGuid()"></see></para>
        /// </summary>
        string TraceIdentifier { get; set; }

        /// <summary>
        /// Indica si la ejecución se considera ligera. Valor por defecto: <c>false</c>.
        /// <para> <c>true</c> : Solo guarda las excepciones. <see cref = "StateJob.Failed"></see>.</para>
        /// <para> <c>false</c> : Registro full de todo el proceso(
        /// <see cref = "StateJob.Enqueued"></see>, 
        /// <see cref = "StateJob.Processing"></see>, 
        /// <see cref = "StateJob.Succeeded"></see>, 
        /// <see cref = "StateJob.Failed"></see> ).</para>
        /// </summary>
        bool IsLightExecution { get; set; }

        /// <summary>
        /// Usuario que ejecuta la tarea en segundo plano.
        /// </summary>
        string CreateUser { get; set; }

        /// <summary>
        /// Función anónima en segundo plano.
        /// </summary>
        Func<IServiceProvider, Task<string>> RunFunction { get; set; }

        /// <summary>
        /// Función anónima para capturar una excepción generada por el proceso en segundo plano.
        /// </summary>
        Func<IServiceProvider, Exception, Task> RunFunctionException { get; set; }

        /// <summary>
        /// Ejecuta la función asignada al trabajador de forma asíncrona <see cref = "RunFunction"></see>.
        /// <para>Esta ejecución encapsula el control de errores, la trazabilidad, el monitoreo del tiempo de ejecución y el comportamiento de auditoría opcional.</para>
        /// <para>Usar este método permite centralizar la lógica de ejecución segura para tareas en segundo plano o procesamiento de eventos en colas.</para>
        /// </summary>
        /// <param name="cancellationToken">Token para cancelar la ejecución anticipadamente, si aplica.</param>
        /// <returns>Una tarea que representa la operación asíncrona.</returns>
        Task ExecuteAsync(CancellationToken cancellationToken = default);

    }

}
