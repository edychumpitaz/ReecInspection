namespace Reec.Inspection.Services
{
    public interface IDbContextService
    {
        InspectionDbContext GetDbContext();
    }
}
