using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Enums;
using SIGO.Security;
using SIGO.Services.Interfaces;

namespace SIGO.Controllers
{
    [ApiController]
    [Route("api/v1/clientes")]
    [Authorize]
    public sealed class ClienteVinculoController : ControllerBase
    {
        private readonly IClienteVinculoService _vinculoService;
        private readonly ICurrentUserService _currentUserService;

        public ClienteVinculoController(
            IClienteVinculoService vinculoService,
            ICurrentUserService currentUserService)
        {
            _vinculoService = vinculoService;
            _currentUserService = currentUserService;
        }

        [HttpGet("me/vinculos")]
        [Authorize(Roles = SystemRoles.Cliente)]
        public async Task<IActionResult> GetMyLinks(CancellationToken cancellationToken)
        {
            var clienteId = _currentUserService.UserId;
            if (!clienteId.HasValue)
                return Forbid();

            var result = await _vinculoService.GetByClientAsync(
                clienteId.Value,
                cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = $"{SystemRoles.Oficina},{SystemRoles.Funcionario}")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting(RateLimitPolicies.ClientePreRegistration)]
        public async Task<IActionResult> RegisterFull(
            [FromBody] PreCadastrarClienteDTO request,
            CancellationToken cancellationToken)
        {
            var oficinaId = _currentUserService.OficinaId;
            if (!oficinaId.HasValue)
                return Forbid();

            var result = await _vinculoService.PreRegisterAsync(
                request,
                oficinaId.Value,
                CreateAuthenticatedAuditContext(),
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, result);
        }

        [HttpDelete("me/vinculos/{oficinaId:int}")]
        [Authorize(Roles = SystemRoles.Cliente)]
        public async Task<IActionResult> RevokeLink(
            int oficinaId,
            CancellationToken cancellationToken)
        {
            var clienteId = _currentUserService.UserId;
            if (!clienteId.HasValue)
                return Forbid();

            await _vinculoService.RevokeAsync(
                clienteId.Value,
                oficinaId,
                CreateAuthenticatedAuditContext(),
                cancellationToken);
            return NoContent();
        }

        private SecurityAuditContext CreateAuthenticatedAuditContext()
        {
            var actorType = _currentUserService.IsInRole(SystemRoles.Cliente)
                ? TipoAtorAuditoria.Cliente
                : _currentUserService.IsInRole(SystemRoles.Funcionario)
                    ? TipoAtorAuditoria.Funcionario
                    : TipoAtorAuditoria.Oficina;

            return new SecurityAuditContext(
                actorType,
                _currentUserService.UserId,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.TraceIdentifier);
        }
    }
}
