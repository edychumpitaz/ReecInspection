namespace Reec.Inspection.Services
{
    public interface IDateTimeService
    {
        public DateTime Now { get; }
        public DateTime UtcNow { get; }
        public TimeZoneInfo TimeZoneInfo { get; } 
    }
}
