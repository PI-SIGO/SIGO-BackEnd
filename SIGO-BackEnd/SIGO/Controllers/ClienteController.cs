using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGO.Errors;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Services.Interfaces;
using SIGO.Security;
using SIGO.Utils;

namespace SIGO.Controllers
{
    [Route("api/v1/clientes")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.SelfServiceAccess)]
    public class ClienteController : ControllerBase
    {
        private readonly IClienteService _clienteService;
        private readonly IClienteAuthenticationService _clienteAuthenticationService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ICurrentUserService _currentUserService;

        public ClienteController(
            IClienteService clienteService,
            IClienteAuthenticationService clienteAuthenticationService,
            IJwtTokenService jwtTokenService,
            ICurrentUserService currentUserService)
        {
            _clienteService = clienteService;
            _clienteAuthenticationService = clienteAuthenticationService;
            _jwtTokenService = jwtTokenService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario}")]
        public async Task<IActionResult> GetAll([FromQuery] PaginationRequest? pagination = null)
        {
            pagination ??= new PaginationRequest();
            if (_currentUserService.IsInRole(SystemRoles.Admin))
            {
                var clientes = await _clienteService.GetAll();
                return Ok(PagedResponse<ClienteDTO>.Create(clientes, pagination));
            }

            var oficinaId = _currentUserService.OficinaId;
            if (!oficinaId.HasValue)
                return Forbid();

            var clientesOficina = await _clienteService.GetByOficina(oficinaId.Value);

            return Ok(PagedResponse<ClienteOficinaDTO>.Create(clientesOficina, pagination));
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario},{SystemRoles.Cliente}")]
        public async Task<IActionResult> GetByIdWithDetails(int id)
        {
            if (_currentUserService.IsInRole(SystemRoles.Cliente) && _currentUserService.UserId != id)
                return Forbid();

            if (_currentUserService.IsInRole(SystemRoles.Admin) || _currentUserService.IsInRole(SystemRoles.Cliente))
            {
                var clienteDto = await _clienteService.GetByIdWithDetails(id);
                if (clienteDto is null)
                    return this.ApiProblem(
                        StatusCodes.Status404NotFound,
                        "Cliente não encontrado.");

                return Ok(clienteDto);
            }

            var oficinaId = _currentUserService.OficinaId;
            if (!oficinaId.HasValue)
                return Forbid();

            var clienteOficinaDto = await _clienteService.GetByIdWithDetailsForOficina(id, oficinaId.Value);

            if (clienteOficinaDto is null)
                return this.ApiProblem(
                    StatusCodes.Status404NotFound,
                    "Cliente não encontrado.");

            return Ok(clienteOficinaDto);
        }

        [HttpGet("nome/{nome}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario}")]
        public async Task<IActionResult> GetByNameWithDetails(
            string nome,
            [FromQuery] PaginationRequest? pagination = null)
        {
            pagination ??= new PaginationRequest();
            if (_currentUserService.IsInRole(SystemRoles.Admin))
            {
                var clientesDto = await _clienteService.GetByNameWithDetails(nome);
                return Ok(PagedResponse<ClienteDTO>.Create(clientesDto, pagination));
            }

            var oficinaId = _currentUserService.OficinaId;
            if (!oficinaId.HasValue)
                return Forbid();

            var clientesOficinaDto = await _clienteService.GetByNameWithDetailsForOficina(nome, oficinaId.Value);

            return Ok(PagedResponse<ClienteOficinaDTO>.Create(clientesOficinaDto, pagination));
        }

        [HttpGet("oficinas/{oficinaId:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario}")]
        public async Task<IActionResult> GetByOficinaId(
            int oficinaId,
            [FromQuery] PaginationRequest? pagination = null)
        {
            pagination ??= new PaginationRequest();
            if (!_currentUserService.IsInRole(SystemRoles.Admin))
            {
                var currentOficinaId = _currentUserService.OficinaId;
                if (!currentOficinaId.HasValue || currentOficinaId.Value != oficinaId)
                    return Forbid();
            }

            var clientes = await _clienteService.GetByOficina(oficinaId);

            return Ok(PagedResponse<ClienteOficinaDTO>.Create(clientes, pagination));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Cliente}")]
        public async Task<IActionResult> Put([FromRoute] int id, [FromBody] ClienteRequestDTO clienteDTO)
        {
            if (_currentUserService.IsInRole(SystemRoles.Cliente) && _currentUserService.UserId != id)
                return Forbid();

            if (clienteDTO is null)
            {
                return this.ApiValidationProblem(
                    StatusCodes.Status400BadRequest,
                    "request",
                    "O corpo da requisição é obrigatório.");
            }

            var existingClienteDTO = await _clienteService.GetById(id);
            if (existingClienteDTO is null)
            {
                return this.ApiProblem(
                    StatusCodes.Status404NotFound,
                    "O cliente informado não existe.");
            }

            SanitizeCliente(clienteDTO);
            await _clienteService.Update(clienteDTO, id);

            return Ok(await _clienteService.GetById(id));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Cliente}")]
        public async Task<IActionResult> Delete(
            int id,
            CancellationToken cancellationToken = default)
        {
            if (_currentUserService.IsInRole(SystemRoles.Cliente) && _currentUserService.UserId != id)
                return Forbid();

            var clienteDTO = await _clienteService.GetById(id);

            if (clienteDTO is null)
            {
                return this.ApiProblem(
                    StatusCodes.Status404NotFound,
                    "Cliente não encontrado.");
            }

            var deactivated = await _clienteService.DeactivateAsync(id, cancellationToken);
            return deactivated
                ? NoContent()
                : this.ApiProblem(StatusCodes.Status404NotFound, "Cliente não encontrado.");
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting(RateLimitPolicies.ClienteLogin)]
        public async Task<ActionResult> Login(
            [FromBody] LoginClienteDTO login,
            CancellationToken cancellationToken)
        {
            if (login is null)
            {
                return this.ApiValidationProblem(
                    StatusCodes.Status400BadRequest,
                    "request",
                    "O corpo da requisição é obrigatório.");
            }

            var cliente = await _clienteAuthenticationService.AuthenticateAsync(login, cancellationToken);

            if (cliente is null)
            {
                return this.ApiProblem(
                    StatusCodes.Status401Unauthorized,
                    "CPF ou senha inválidos.");
            }

            var token = _jwtTokenService.GenerateToken(new JwtTokenRequest
            {
                UserId = cliente.ClienteId,
                Name = cliente.Nome,
                Email = cliente.Email,
                Role = SystemRoles.Cliente,
                TokenVersion = cliente.TokenVersion
            });
            return Ok(new AccessTokenResponse(token));
        }

        [HttpPut("me/senha")]
        [Authorize(Roles = SystemRoles.Cliente)]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting(RateLimitPolicies.ClientePasswordChange)]
        public async Task<IActionResult> ChangePassword(
            [FromBody] AlterarSenhaClienteDTO request,
            CancellationToken cancellationToken)
        {
            var clienteId = _currentUserService.UserId;
            if (!clienteId.HasValue)
                return Forbid();

            await _clienteAuthenticationService.ChangePasswordAsync(
                clienteId.Value,
                request,
                new SecurityAuditContext(
                    SIGO.Objects.Enums.TipoAtorAuditoria.Cliente,
                    clienteId.Value,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    HttpContext.TraceIdentifier),
                cancellationToken);

            return NoContent();
        }

        private static void SanitizeCliente(ClienteRequestDTO clienteDTO)
        {
            clienteDTO.Cpf_Cnpj = SanitizeHelper.ApenasDigitos(clienteDTO.Cpf_Cnpj);
            clienteDTO.Cep = SanitizeHelper.ApenasDigitos(clienteDTO.Cep);

            if (clienteDTO.Telefones == null)
                return;

            foreach (var telefone in clienteDTO.Telefones)
            {
                telefone.Numero = SanitizeHelper.ApenasDigitos(telefone.Numero);
            }
        }

    }
}
