using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Enums;
using SIGO.Security;
using SIGO.Services.Interfaces;

namespace SIGO.Controllers
{
    [ApiController]
    [Route("api/v1/clientes/cadastros")]
    public sealed class ClienteRegistrationController : ControllerBase
    {
        private readonly IClienteRegistrationService _registrationService;

        public ClienteRegistrationController(IClienteRegistrationService registrationService)
        {
            _registrationService = registrationService;
        }

        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.PublicRegistration)]
        public async Task<IActionResult> Register(
            [FromBody] CadastrarClienteDTO request,
            CancellationToken cancellationToken)
        {
            var result = await _registrationService.RegisterAsync(
                request,
                new SecurityAuditContext(
                    TipoAtorAuditoria.Anonimo,
                    null,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    HttpContext.TraceIdentifier),
                cancellationToken);

            return Created($"/api/v1/clientes/{result.ClienteId}", result);
        }
    }
}
