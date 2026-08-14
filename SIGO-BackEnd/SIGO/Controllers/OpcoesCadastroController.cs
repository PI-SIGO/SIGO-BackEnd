using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGO.Objects.Dtos.Entities;
using SIGO.Security;
using SIGO.Services.Interfaces;

namespace SIGO.Controllers
{
    [ApiController]
    [Route("api/v1/opcoes-cadastro")]
    [Authorize(Roles = $"{SystemRoles.Oficina},{SystemRoles.Funcionario}")]
    public class OpcoesCadastroController : ControllerBase
    {
        private readonly IOpcoesCadastroService _service;
        private readonly ICurrentUserService _currentUserService;

        public OpcoesCadastroController(
            IOpcoesCadastroService service,
            ICurrentUserService currentUserService)
        {
            _service = service;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(OpcoesCadastroDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<OpcoesCadastroDTO>> Get(
            CancellationToken cancellationToken = default)
        {
            if (!_currentUserService.OficinaId.HasValue)
                return Forbid();

            var options = await _service.GetByOficinaAsync(
                _currentUserService.OficinaId.Value,
                cancellationToken);

            return Ok(options);
        }
    }
}
