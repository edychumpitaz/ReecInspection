using static Reec.Inspection.ReecEnums;

namespace Reec.Inspection.Entities
{
    public sealed class LogJob
    {
        public int IdLogJob { get; set; }
        public string ApplicationName { get; set; }
        public string NameJob { get; set; }
        public StateJob? StateJob { get; set; }
        public string TraceIdentifier { get; set; }
        public TimeSpan? Duration { get; set; }
        public string Exception { get; set; }
        public string InnerException { get; set; }
        public string StackTrace { get; set; }
        public Dictionary<string, object> Data { get; set; }
        public string Message { get; set; }
        public DateOnly? CreateDateOnly { get; set; }
        public string CreateUser { get; set; }
        public DateTime CreateDate { get; set; }
    }

}
