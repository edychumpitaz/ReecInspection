using Microsoft.EntityFrameworkCore;
using Reec.Inspection.Entities;
using System.Diagnostics.CodeAnalysis;

namespace Reec.Inspection
{
    public class InspectionDbContext : DbContext
    {
        public DbSet<LogHttp> LogHttp { get; set; }

        public InspectionDbContext([NotNull] DbContextOptions options) : base(options)
        {
        }

    }


}
