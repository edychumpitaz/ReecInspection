using Microsoft.EntityFrameworkCore;
using Reec.Inspection.Entities;
using System.Diagnostics.CodeAnalysis;

namespace Reec.Inspection
{
    public class InspectionDbContext : DbContext
    {
        public DbSet<LogAudit> LogAudits { get; set; }
        public DbSet<LogDb> LogDbs { get; set; }
        public DbSet<LogEndpoint> LogEndpoints { get; set; }
        public DbSet<LogHttp> LogHttps { get; set; }
        public DbSet<LogJob> LogJobs { get; set; }

        public void DetachAllEntities()
        {
            var attachedEntities = this.ChangeTracker.Entries()
                                    .Where(e => e.State != EntityState.Detached)
                                    .Select(e => e.Entity).ToList();
            foreach (var entity in attachedEntities)
            {
                var entry = this.Entry(entity);
                if (entry.State != EntityState.Detached)
                    entry.State = EntityState.Detached;
            }
        }
        public void DetachEntity<TEntity>(TEntity entity) where TEntity : class
        {
            var entry = this.Entry(entity);
            if (entry.State != EntityState.Detached)
                entry.State = EntityState.Detached;
        }
        public InspectionDbContext([NotNull] DbContextOptions options) : base(options)
        {
        }


    }


}
