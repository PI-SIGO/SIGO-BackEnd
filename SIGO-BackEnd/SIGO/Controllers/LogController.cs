using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGO.Middleware;
using SIGO.Security;

namespace SIGO.Controllers
{
    [Route("api/logs")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.SelfServiceAccess)]
    public class LogController : ControllerBase
    {
        private readonly IRequestLogStore _logStore;

        public LogController(IRequestLogStore logStore)
        {
            _logStore = logStore;
        }

        [HttpGet("recent")]
        public IActionResult GetRecent([FromQuery] int limit = 50)
        {
            var safeLimit = Math.Clamp(limit, 1, 200);
            var logs = FilterForCurrentUser(_logStore.GetRecent(safeLimit));
            return Ok(logs);
        }

        [HttpGet("{correlationId}")]
        public IActionResult GetByCorrelationId(string correlationId)
        {
            var log = _logStore.GetByCorrelationId(correlationId);
            if (log is null)
                return NotFound(new { message = "Log não encontrado." });

            if (CanSeeAllLogs())
                return Ok(log);

            var currentUser = GetCurrentUserName();
            if (!log.User.Equals(currentUser, StringComparison.OrdinalIgnoreCase))
                return Forbid();

            return Ok(log);
        }

        private IEnumerable<RequestLogEntry> FilterForCurrentUser(IReadOnlyCollection<RequestLogEntry> logs)
        {
            if (CanSeeAllLogs())
                return logs;

            var currentUser = GetCurrentUserName();
            return logs.Where(x => x.User.Equals(currentUser, StringComparison.OrdinalIgnoreCase));
        }

        private bool CanSeeAllLogs()
        {
            return User.IsInRole(SystemRoles.Admin)
                || User.IsInRole(SystemRoles.Funcionario)
                || User.IsInRole(SystemRoles.Oficina);
        }

        private string GetCurrentUserName()
        {
            return User.Identity?.Name ?? string.Empty;
        }
    }
}
