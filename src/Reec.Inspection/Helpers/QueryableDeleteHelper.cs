using Microsoft.EntityFrameworkCore;

namespace Reec.Inspection.Helpers
{
    /// <summary>
    /// Helper interno para manejar la compatibilidad de ExecuteDeleteAsync entre versiones de EF Core.
    /// </summary>
    /// <remarks>
    /// En EF Core 8, ExecuteDeleteAsync se encuentra en <see cref="RelationalQueryableExtensions"/>.
    /// A partir de EF Core 9, el método se movió a <see cref="EntityFrameworkQueryableExtensions"/>.
    /// </remarks>
    internal static class QueryableDeleteHelper
    {
        /// <summary>
        /// Ejecuta una eliminación asíncrona de los registros que coinciden con la consulta.
        /// </summary>
        /// <typeparam name="T">Tipo de entidad</typeparam>
        /// <param name="query">Consulta IQueryable</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Número de filas eliminadas</returns>
        public static Task<int> ExecuteDeleteAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default) where T : class
        {
#if NET9_0_OR_GREATER
            // EF Core 9+ tiene ExecuteDeleteAsync en EntityFrameworkQueryableExtensions
            return EntityFrameworkQueryableExtensions.ExecuteDeleteAsync(query, cancellationToken);
#else
            // EF Core 8 tiene ExecuteDeleteAsync en RelationalQueryableExtensions
            return RelationalQueryableExtensions.ExecuteDeleteAsync(query, cancellationToken);
#endif
        }
    }
}
