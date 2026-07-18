using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGO.Errors;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Security;
using SIGO.Services.Interfaces;
using SIGO.Utils;

namespace SIGO.Controllers
{
    [Route("api/v1/telefones")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.SelfServiceAccess)]
    public class TelefoneController : ControllerBase
    {
        private readonly ITelefoneService _telefoneService;
        private readonly IClienteService _clienteService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public TelefoneController(
            ITelefoneService telefoneService,
            IClienteService clienteService,
            IMapper mapper,
            ICurrentUserService currentUserService)
        {
            _telefoneService = telefoneService;
            _clienteService = clienteService;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario},{SystemRoles.Cliente}")]
        public async Task<IActionResult> Get(int id)
        {
            var telefoneDto = await _telefoneService.GetById(id);

            if (telefoneDto is null)
                return this.ApiProblem(
                    StatusCodes.Status404NotFound,
                    "Telefone não encontrado.");

            if (_currentUserService.IsInRole(SystemRoles.Cliente) && _currentUserService.UserId != telefoneDto.ClienteId)
                return Forbid();

            if (IsTenantUser() && !await ClientePermiteTelefones(telefoneDto.ClienteId))
                return Forbid();

            return Ok(telefoneDto);
        }

        [HttpGet("nome/{nome}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario}")]
        public async Task<IActionResult> GetByNameWithDetails(
            string nome,
            [FromQuery] PaginationRequest? pagination = null)
        {
            IEnumerable<TelefoneDTO> clientesDto;
            if (_currentUserService.IsInRole(SystemRoles.Admin))
            {
                clientesDto = await _telefoneService.GetTelefoneByNome(nome);
            }
            else
            {
                var oficinaId = _currentUserService.OficinaId;
                if (!oficinaId.HasValue)
                    return Forbid();

                clientesDto = await _telefoneService.GetTelefoneByNomeForOficina(nome, oficinaId.Value);
            }

            return Ok(PagedResponse<TelefoneDTO>.Create(clientesDto, pagination ?? new PaginationRequest()));
        }

        [HttpPost]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Cliente}")]
        public async Task<IActionResult> Post(TelefoneDTO telefoneDTO)
        {
            if (!_currentUserService.IsInRole(SystemRoles.Admin) && !_currentUserService.IsInRole(SystemRoles.Cliente))
                return Forbid();

            if (telefoneDTO is null)
            {
                return this.ApiValidationProblem(
                    StatusCodes.Status400BadRequest,
                    "request",
                    "O corpo da requisição é obrigatório.");
            }

            telefoneDTO.Id = 0;
            SanitizeTelefone(telefoneDTO);

            if (_currentUserService.IsInRole(SystemRoles.Cliente))
            {
                var clienteId = _currentUserService.UserId;
                if (!clienteId.HasValue || telefoneDTO.ClienteId != clienteId.Value)
                    return Forbid();
            }
            else if (IsTenantUser() && !await ClientePermiteTelefones(telefoneDTO.ClienteId))
            {
                return Forbid();
            }

            var telefoneCriado = await _telefoneService.CreateTelefone(telefoneDTO);
            return Created($"/api/v1/telefones/{telefoneCriado.Id}", telefoneCriado);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Cliente}")]
        public async Task<IActionResult> Put(int id, TelefoneDTO telefoneDTO)
        {
            if (!_currentUserService.IsInRole(SystemRoles.Admin) && !_currentUserService.IsInRole(SystemRoles.Cliente))
                return Forbid();

            if (telefoneDTO is null)
            {
                return this.ApiValidationProblem(
                    StatusCodes.Status400BadRequest,
                    "request",
                    "O corpo da requisição é obrigatório.");
            }

            var existingTelefoneDTO = await _telefoneService.GetById(id);
            if (existingTelefoneDTO is null)
            {
                return this.ApiProblem(
                    StatusCodes.Status404NotFound,
                    "O telefone informado não existe.");
            }

            if (_currentUserService.IsInRole(SystemRoles.Cliente) && _currentUserService.UserId != existingTelefoneDTO.ClienteId)
                return Forbid();

            if (IsTenantUser() && !await ClientePermiteTelefones(existingTelefoneDTO.ClienteId))
                return Forbid();

            SanitizeTelefone(telefoneDTO);

            if (_currentUserService.IsInRole(SystemRoles.Cliente))
            {
                var clienteId = _currentUserService.UserId;
                if (!clienteId.HasValue || telefoneDTO.ClienteId != clienteId.Value)
                    return Forbid();
            }
            else if (IsTenantUser() && !await ClientePermiteTelefones(telefoneDTO.ClienteId))
            {
                return Forbid();
            }

            var telefoneAtualizado = await _telefoneService.UpdateTelefone(telefoneDTO, id);
            return Ok(telefoneAtualizado);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Cliente}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!_currentUserService.IsInRole(SystemRoles.Admin) && !_currentUserService.IsInRole(SystemRoles.Cliente))
                return Forbid();

            var telefoneDTO = await _telefoneService.GetById(id);

            if (telefoneDTO is null)
            {
                return this.ApiProblem(
                    StatusCodes.Status404NotFound,
                    "Telefone não encontrado.");
            }

            if (_currentUserService.IsInRole(SystemRoles.Cliente) && _currentUserService.UserId != telefoneDTO.ClienteId)
                return Forbid();

            if (IsTenantUser() && !await ClientePermiteTelefones(telefoneDTO.ClienteId))
                return Forbid();

            await _telefoneService.Remove(id);

            return NoContent();
        }

        private static void SanitizeTelefone(TelefoneDTO telefoneDTO)
        {
            telefoneDTO.Numero = SanitizeHelper.ApenasDigitos(telefoneDTO.Numero);
        }

        private bool IsTenantUser()
        {
            return _currentUserService.IsInRole(SystemRoles.Oficina) ||
                _currentUserService.IsInRole(SystemRoles.Funcionario);
        }

        private async Task<bool> ClientePermiteTelefones(int clienteId)
        {
            var oficinaId = _currentUserService.OficinaId;
            return oficinaId.HasValue &&
                await _clienteService.ExistsInOficina(clienteId, oficinaId.Value);
        }

    }
}
