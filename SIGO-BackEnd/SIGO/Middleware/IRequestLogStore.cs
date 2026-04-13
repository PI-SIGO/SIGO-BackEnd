namespace SIGO.Middleware
{
    public interface IRequestLogStore
    {
        void Add(RequestLogEntry entry);
        IReadOnlyCollection<RequestLogEntry> GetRecent(int limit);
        RequestLogEntry? GetByCorrelationId(string correlationId);
    }
}
