using Microsoft.Extensions.Primitives;

namespace SIGO.Middleware
{
    public class CorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-ID";
        public const string ItemName = "CorrelationId";

        private const int MaxCorrelationIdLength = 128;
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = ResolveCorrelationId(context.Request.Headers);
            context.TraceIdentifier = correlationId;
            context.Items[ItemName] = correlationId;

            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = correlationId;
                return Task.CompletedTask;
            });

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            }))
            {
                await _next(context);
            }
        }

        private static string ResolveCorrelationId(IHeaderDictionary headers)
        {
            if (headers.TryGetValue(HeaderName, out var values) &&
                TryGetValidHeaderValue(values, out var correlationId))
            {
                return correlationId;
            }

            return Guid.NewGuid().ToString("N");
        }

        private static bool TryGetValidHeaderValue(StringValues values, out string correlationId)
        {
            correlationId = values.FirstOrDefault()?.Trim() ?? string.Empty;

            if (correlationId.Length == 0 || correlationId.Length > MaxCorrelationIdLength)
                return false;

            foreach (var character in correlationId)
            {
                if (!char.IsLetterOrDigit(character) &&
                    character != '-' &&
                    character != '_' &&
                    character != '.' &&
                    character != ':')
                {
                    correlationId = string.Empty;
                    return false;
                }
            }

            return true;
        }
    }
}
