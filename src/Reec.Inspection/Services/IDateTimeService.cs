namespace Reec.Inspection.Services
{
    /// <summary>
    /// Servicio encargado de proporcionar información de fecha y hora según la configuración regional del sistema definida en <see cref="ReecExceptionOptions.SystemTimeZoneId"/>.
    /// </summary>
    /// <remarks>
    /// Esta interfaz permite obtener fechas y horas coherentes con la zona horaria configurada,
    /// garantizando consistencia temporal entre los procesos en segundo plano, auditorías y registros del sistema.
    ///
    /// <para>
    /// Implementaciones comunes como <see cref="DateTimeService"/> convierten automáticamente la hora UTC
    /// a la zona local definida por la aplicación, utilizando internamente <see cref="TimeZoneInfo.ConvertTime(DateTime, TimeZoneInfo)"/>.
    /// </para>
    ///
    /// <example>
    /// Ejemplo de uso:
    /// <code>
    /// var dateTime = serviceProvider.GetRequiredService&lt;IDateTimeService&gt;();
    /// var nowLocal = dateTime.Now;     // Hora local según configuración.
    /// var nowUtc   = dateTime.UtcNow;  // Hora universal (UTC).
    /// </code>
    /// </example>
    /// </remarks>
    public interface IDateTimeService
    {
        /// <summary>
        /// Obtiene la fecha y hora actual del sistema ajustada a la zona horaria configurada.
        /// </summary>
        /// <remarks>
        /// Retorna el valor equivalente a <c>TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo)</c>.
        /// Todas las operaciones de log y auditoría registran esta hora como referencia local.
        /// </remarks>
        DateTime Now { get; }

        /// <summary>
        /// Obtiene la fecha y hora actual en formato UTC (Tiempo Universal Coordinado).
        /// </summary>
        /// <remarks>
        /// Este valor no se ajusta a ninguna zona horaria.
        /// Se utiliza internamente para cálculos y conversiones mediante <see cref="TimeZoneInfo"/>.
        /// </remarks>
        DateTime UtcNow { get; }

        /// <summary>
        /// Obtiene la instancia de <see cref="TimeZoneInfo"/> utilizada por la aplicación.
        /// </summary>
        /// <remarks>
        /// Corresponde al identificador configurado en <see cref="ReecExceptionOptions.SystemTimeZoneId"/>.
        /// Puedes obtener la lista completa de zonas disponibles mediante:
        /// <see cref="TimeZoneInfo.GetSystemTimeZones()"/>.
        /// </remarks>
        TimeZoneInfo TimeZoneInfo { get; }
    }

}
