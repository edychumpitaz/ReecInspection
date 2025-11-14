using static Reec.Inspection.ReecEnums;
namespace Reec.Inspection.Workers
{

    /// <summary>
    /// Interfaz para ejecutar procesos en segundo plano de forma segura y trazable,
    /// sin necesidad de bloquear el hilo que dispara la operación.
    /// </summary>
    /// <remarks>
    /// Centraliza control de errores, trazabilidad (trace id), tiempos de ejecución y
    /// persistencia opcional del resultado/estado del job.
    /// </remarks>
    public interface IWorker : IDisposable
    {
        /// <summary>
        /// Tiempo a esperar antes de iniciar la ejecución del proceso en segundo plano.
        /// </summary>
        /// <remarks>
        /// Útil para diferir la ejecución (por ejemplo, reintentos, backoff o coordinación con otros procesos).
        /// Si es <see langword="null"/>, la ejecución inicia inmediatamente.
        /// </remarks>
        TimeSpan? Delay { get; set; }

        /// <summary>
        /// Nombre lógico del trabajo en segundo plano.
        /// </summary>
        /// <remarks>
        /// Se usa en logs y auditoría para identificar claramente la tarea.
        /// Valor por defecto: <c>"Anonymous"</c>.
        /// </remarks>
        string NameJob { get; set; }

        /// <summary>
        /// Identificador de trazabilidad asociado al trabajo.
        /// </summary>
        /// <remarks>
        /// Si el job se origina desde una petición HTTP, se tomará
        /// <see cref="Microsoft.AspNetCore.Http.HttpContext.TraceIdentifier"/>.
        /// En caso contrario, se generará un identificador nuevo (por ejemplo, <c>FastGuid.NewGuid()</c>).
        /// Este valor permite correlacionar logs entre front, API, jobs y otros servicios.
        /// </remarks>
        string TraceIdentifier { get; set; }

        /// <summary>
        /// Indica si la ejecución es “ligera”.
        /// </summary>
        /// <remarks>
        /// Cuando es <see langword="true"/>, solo se registran excepciones
        /// (estado <see cref="StateJob.Failed"/>).
        /// Cuando es <see langword="false"/>, se registra el ciclo completo:
        /// <see cref="StateJob.Enqueued"/>, <see cref="StateJob.Processing"/>,
        /// <see cref="StateJob.Succeeded"/> y <see cref="StateJob.Failed"/>.
        /// Útil para reducir costo de almacenamiento en jobs muy frecuentes.
        /// </remarks>
        bool IsLightExecution { get; set; }

        /// <summary>
        /// Usuario responsable o que originó la ejecución del trabajo.
        /// </summary>
        /// <remarks>
        /// Puede mapear al usuario autenticado, a una cuenta técnica (service account)
        /// o a un identificador de sistema externo.
        /// Se persiste para auditoría y trazabilidad.
        /// </remarks>
        string CreateUser { get; set; }

        /// <summary>
        /// Lógica principal a ejecutar en segundo plano.
        /// </summary>
        /// <remarks>
        /// Recibe el <see cref="IServiceProvider"/> para resolver dependencias del contenedor (scoped/transient/singleton).
        /// Debe devolver un <see cref="string"/> opcional (mensaje, id de operación, etc.) que se guardará como resultado.
        /// Si lanza una excepción, se manejará por <see cref="RunFunctionException"/> si está configurado;
        /// de lo contrario, se registrará como <see cref="StateJob.Failed"/>.
        /// </remarks>
        Func<IServiceProvider, Task<string>> RunFunction { get; set; }

        /// <summary>
        /// Callback para tratamiento de excepciones lanzadas por <see cref="RunFunction"/>.
        /// </summary>
        /// <remarks>
        /// Permite aplicar lógica de recuperación, notificación o métricas adicionales.
        /// Siempre se registra la excepción en la traza del job antes de invocar este callback.
        /// </remarks>
        Func<IServiceProvider, Exception, Task> RunFunctionException { get; set; }

        /// <summary>
        /// Ejecuta la función configurada en <see cref="RunFunction"/> de manera segura y asíncrona.
        /// </summary>
        /// <remarks>
        /// Encapsula trazabilidad, control de errores, medición de duración y
        /// persistencia opcional del estado del job (ligero o completo).
        /// Respeta <see cref="Delay"/> si está configurado.
        /// </remarks>
        /// <param name="cancellationToken">Token para solicitar cancelación anticipada.</param>
        /// <returns>Tarea que representa la operación asíncrona.</returns>
        Task ExecuteAsync(CancellationToken cancellationToken = default);
    }

}
