namespace Reec.Inspection.Entities
{
    public class LogEndpoint 
    {
        public int IdLogEndpoint { get; set; }
        public string ApplicationName { get; set; }
        public int HttpStatusCode { get; set; }
        public TimeSpan Duration { get; set; }
        public string TraceIdentifier { get; set; }
        public byte Retry { get; set; }
        public string Method { get; set; }
        public string Schema { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string HostPort { get; set; }
        public string Path { get; set; }
        public string QueryString { get; set; }
        public Dictionary<string, string> RequestHeader { get; set; }
        public string RequestBody { get; set; }
        public Dictionary<string, string> ResponseHeader { get; set; }
        public string ResponseBody { get; set; }
        public DateOnly CreateDateOnly { get; set; }
        public string CreateUser { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
