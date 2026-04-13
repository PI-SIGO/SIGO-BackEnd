using System.Diagnostics;

namespace SIGO.Middleware
{
    public class RequestAuditLoggingMiddleware
    {
        private const string CorrelationIdHeader = "X-Correlation-Id";
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestAuditLoggingMiddleware> _logger;
        private readonly IRequestLogStore _logStore;

        public RequestAuditLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestAuditLoggingMiddleware> logger,
            IRequestLogStore logStore)
        {
            _next = next;
            _logger = logger;
            _logStore = logStore;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = Guid.NewGuid().ToString("N");
            }

            context.Response.Headers[CorrelationIdHeader] = correlationId;

            var stopwatch = Stopwatch.StartNew();

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            }))
            {
                var user = context.User?.Identity?.IsAuthenticated == true
                    ? context.User.Identity?.Name ?? "authenticated"
                    : "anonymous";

                try
                {
                    await _next(context);
                    stopwatch.Stop();

                    _logStore.Add(new RequestLogEntry
                    {
                        TimestampUtc = DateTimeOffset.UtcNow,
                        CorrelationId = correlationId,
                        Method = context.Request.Method,
                        Path = context.Request.Path.Value ?? string.Empty,
                        StatusCode = context.Response.StatusCode,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        User = user,
                        IsError = context.Response.StatusCode >= 500
                    });

                    _logger.LogInformation(
                        "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms. User: {User}",
                        context.Request.Method,
                        context.Request.Path.Value,
                        context.Response.StatusCode,
                        stopwatch.ElapsedMilliseconds,
                        user);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    _logStore.Add(new RequestLogEntry
                    {
                        TimestampUtc = DateTimeOffset.UtcNow,
                        CorrelationId = correlationId,
                        Method = context.Request.Method,
                        Path = context.Request.Path.Value ?? string.Empty,
                        StatusCode = StatusCodes.Status500InternalServerError,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        User = user,
                        IsError = true
                    });

                    _logger.LogError(
                        ex,
                        "Unhandled exception at {Method} {Path} after {ElapsedMs}ms.",
                        context.Request.Method,
                        context.Request.Path.Value,
                        stopwatch.ElapsedMilliseconds);
                    throw;
                }
            }
        }
    }
}
