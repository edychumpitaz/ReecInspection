using Microsoft.EntityFrameworkCore;
using Reec.Inspection.Entities;
using System.Diagnostics.CodeAnalysis;

namespace Reec.Inspection
{
    public class InspectionDbContext : DbContext
    {
        public DbSet<LogAudit> LogAudit { get; set; }
        public DbSet<LogDb> LogDb { get; set; }
        public DbSet<LogEndpoint> LogEndpoint { get; set; }
        public DbSet<LogHttp> LogHttp { get; set; }
        public DbSet<LogJob> LogJob { get; set; }
         
        public InspectionDbContext([NotNull] DbContextOptions options) : base(options)
        {
        }

    }


}
