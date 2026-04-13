using System.Collections.Concurrent;

namespace SIGO.Middleware
{
    public class InMemoryRequestLogStore : IRequestLogStore
    {
        private readonly ConcurrentQueue<RequestLogEntry> _entries = new();
        private readonly int _maxEntries;

        public InMemoryRequestLogStore(IConfiguration configuration)
        {
            var configuredMax = configuration.GetValue<int?>("LogManagement:MaxEntries");
            _maxEntries = configuredMax is > 0 ? configuredMax.Value : 1000;
        }

        public void Add(RequestLogEntry entry)
        {
            _entries.Enqueue(entry);
            while (_entries.Count > _maxEntries)
            {
                _entries.TryDequeue(out _);
            }
        }

        public IReadOnlyCollection<RequestLogEntry> GetRecent(int limit)
        {
            if (limit <= 0)
                return Array.Empty<RequestLogEntry>();

            var data = _entries.ToArray();
            return data
                .Reverse()
                .Take(limit)
                .ToArray();
        }

        public RequestLogEntry? GetByCorrelationId(string correlationId)
        {
            if (string.IsNullOrWhiteSpace(correlationId))
                return null;

            return _entries
                .Reverse()
                .FirstOrDefault(x => x.CorrelationId.Equals(correlationId, StringComparison.OrdinalIgnoreCase));
        }
    }
}
