namespace Reec.Inspection.Services
{
    public class DbContextService<TDbContext> : IDbContextService
                            where TDbContext : InspectionDbContext
    {
        private readonly TDbContext _dbContext;
        public DbContextService(TDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public InspectionDbContext GetDbContext() 
        {
            return _dbContext;
        }
    }
}
