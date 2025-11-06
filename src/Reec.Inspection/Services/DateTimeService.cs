using Reec.Inspection.Options;

namespace Reec.Inspection.Services
{
    public class DateTimeService : IDateTimeService
    {
        private readonly TimeZoneInfo _timeZone;

        public DateTimeService(ReecExceptionOptions options)
        {
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.SystemTimeZoneId);
        }
        public DateTime Now => TimeZoneInfo.ConvertTime(DateTime.UtcNow, _timeZone);

        public DateTime UtcNow => DateTime.UtcNow;

        public TimeZoneInfo TimeZoneInfo => _timeZone;
    }
}
