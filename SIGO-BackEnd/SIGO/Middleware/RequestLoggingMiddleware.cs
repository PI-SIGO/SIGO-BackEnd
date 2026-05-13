using System.Diagnostics;
using Microsoft.AspNetCore.Routing;

namespace SIGO.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var startTimestamp = Stopwatch.GetTimestamp();

            try
            {
                await _next(context);
            }
            finally
            {
                var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
                var statusCode = context.Response.StatusCode;
                var logLevel = GetLogLevel(statusCode);

                _logger.Log(
                    logLevel,
                    "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {ElapsedMilliseconds:0.0000} ms",
                    context.Request.Method,
                    GetRequestPath(context),
                    statusCode,
                    elapsed.TotalMilliseconds);
            }
        }

        private static LogLevel GetLogLevel(int statusCode)
        {
            if (statusCode >= StatusCodes.Status500InternalServerError)
                return LogLevel.Error;

            if (statusCode >= StatusCodes.Status400BadRequest)
                return LogLevel.Warning;

            return LogLevel.Information;
        }

        private static string GetRequestPath(HttpContext context)
        {
            if (context.GetEndpoint() is RouteEndpoint routeEndpoint)
                return routeEndpoint.RoutePattern.RawText ?? routeEndpoint.DisplayName ?? "/";

            return RedactUnmatchedPath(context.Request.Path.Value);
        }

        private static string RedactUnmatchedPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "/";

            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length == 0)
                return "/";

            return "/" + string.Join("/", segments.Select(RedactPathSegment));
        }

        private static string RedactPathSegment(string segment)
        {
            if (segment.Length > 64)
                return "{value}";

            if (Guid.TryParse(segment, out _))
                return "{id}";

            if (segment.All(char.IsDigit))
                return "{id}";

            if (segment.Contains('@') || segment.Contains("%40", StringComparison.OrdinalIgnoreCase))
                return "{value}";

            if (LooksLikeToken(segment))
                return "{token}";

            return segment;
        }

        private static bool LooksLikeToken(string segment)
        {
            if (segment.Length < 32)
                return false;

            return segment.All(character =>
                char.IsLetterOrDigit(character) ||
                character == '-' ||
                character == '_' ||
                character == '.');
        }
    }
}
