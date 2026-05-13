using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Security;
using SIGO.Services.Interfaces;
using SIGO.Validation;

namespace SIGO.Controllers
{
    [Route("api/clientes/compartilhamentos")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.SelfServiceAccess)]
    public class CompartilhamentoClienteController : ControllerBase
    {
        private readonly ICompartilhamentoClienteService _compartilhamentoClienteService;
        private readonly ICurrentUserService _currentUserService;
        private readonly Response _response;

        public CompartilhamentoClienteController(
            ICompartilhamentoClienteService compartilhamentoClienteService,
            ICurrentUserService currentUserService)
        {
            _compartilhamentoClienteService = compartilhamentoClienteService;
            _currentUserService = currentUserService;
            _response = new Response();
        }

        [HttpPost]
        [Authorize(Roles = SystemRoles.Cliente)]
        public async Task<IActionResult> CriarCodigo([FromBody] CriarCompartilhamentoClienteDTO dto)
        {
            var clienteId = _currentUserService.UserId;
            if (!clienteId.HasValue)
                return Forbid();

            try
            {
                var codigo = await _compartilhamentoClienteService.CriarCodigo(clienteId.Value, dto);
                _response.Code = ResponseEnum.SUCCESS;
                _response.Data = codigo;
                _response.Message = "Código de compartilhamento gerado com sucesso";
                return Ok(_response);
            }
            catch (BusinessValidationException ex)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Data = ex.Errors;
                _response.Message = "Dados inválidos";
                return BadRequest(_response);
            }
        }

        [HttpPost("resgatar")]
        [EnableRateLimiting(RateLimitPolicies.CompartilhamentoClienteResgate)]
        [Authorize(Roles = $"{SystemRoles.Oficina},{SystemRoles.Funcionario}")]
        public async Task<IActionResult> ResgatarCodigo([FromBody] ResgatarCompartilhamentoClienteDTO dto)
        {
            var oficinaId = _currentUserService.OficinaId;
            if (!oficinaId.HasValue)
                return Forbid();

            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var dados = await _compartilhamentoClienteService.ResgatarCodigo(oficinaId.Value, dto, ipAddress);
                _response.Code = ResponseEnum.SUCCESS;
                _response.Data = dados;
                _response.Message = "Compartilhamento resgatado com sucesso";
                return Ok(_response);
            }
            catch (BusinessValidationException ex)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Data = ex.Errors;
                _response.Message = "Dados inválidos";
                return BadRequest(_response);
            }
            catch (KeyNotFoundException)
            {
                _response.Code = ResponseEnum.NOT_FOUND;
                _response.Data = null;
                _response.Message = "Código inválido ou expirado.";
                return NotFound(_response);
            }
        }
    }
}
