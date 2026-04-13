namespace SIGO.Middleware
{
    public class RequestLogEntry
    {
        public DateTimeOffset TimestampUtc { get; init; }
        public string CorrelationId { get; init; } = string.Empty;
        public string Method { get; init; } = string.Empty;
        public string Path { get; init; } = string.Empty;
        public int StatusCode { get; init; }
        public long DurationMs { get; init; }
        public string User { get; init; } = "anonymous";
        public bool IsError { get; init; }
    }
}
