using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Security;
using SIGO.Services.Interfaces;
using SIGO.Utils;

namespace SIGO.Controllers
{
    [Route("api/telefones")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.SelfServiceAccess)]
    public class TelefoneController : ControllerBase
    {
        private readonly ITelefoneService _telefoneService;
        private readonly IClienteService _clienteService;
        private readonly ICurrentUserService _currentUserService;
        private readonly Response _response;
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
            _response = new Response();
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario},{SystemRoles.Cliente}")]
        public async Task<IActionResult> Get(int id)
        {
            var telefoneDto = await _telefoneService.GetById(id);

            if (telefoneDto is null)
                return NotFound(new { Message = "Telefone não encontrado" });

            if (_currentUserService.IsInRole(SystemRoles.Cliente) && _currentUserService.UserId != telefoneDto.ClienteId)
                return Forbid();

            if (IsTenantUser() && !await ClientePermiteTelefones(telefoneDto.ClienteId))
                return Forbid();

            return Ok(telefoneDto);
        }

        [HttpGet("nome/{nome}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario}")]
        public async Task<IActionResult> GetByNameWithDetails(string nome)
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

            if (!clientesDto.Any())
                return NotFound(new { Message = "Nenhum cliente encontrado com esse nome" });

            return Ok(clientesDto);
        }

        [HttpPost]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Cliente}")]
        public async Task<IActionResult> Post(TelefoneDTO telefoneDTO)
        {
            if (!_currentUserService.IsInRole(SystemRoles.Admin) && !_currentUserService.IsInRole(SystemRoles.Cliente))
                return Forbid();

            if (telefoneDTO is null)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Data = null;
                _response.Message = "Dados inválidos";

                return BadRequest(_response);
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

            await _telefoneService.Create(telefoneDTO);

            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = telefoneDTO;
            _response.Message = "Telefone cadastrado com sucesso";

            return Ok(_response);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Cliente}")]
        public async Task<IActionResult> Put(int id, TelefoneDTO telefoneDTO)
        {
            if (!_currentUserService.IsInRole(SystemRoles.Admin) && !_currentUserService.IsInRole(SystemRoles.Cliente))
                return Forbid();

            if (telefoneDTO is null)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Data = null;
                _response.Message = "Dados inválidos";

                return BadRequest(_response);
            }

            var existingTelefoneDTO = await _telefoneService.GetById(id);
            if (existingTelefoneDTO is null)
            {
                _response.Code = ResponseEnum.NOT_FOUND;
                _response.Data = null;
                _response.Message = "O telefone informado não existe";
                return NotFound(_response);
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

            await _telefoneService.Update(telefoneDTO, id);

            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = telefoneDTO;
            _response.Message = "Telefone atualizado com sucesso";

            return Ok(_response);
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
                _response.Code = ResponseEnum.NOT_FOUND;
                _response.Data = null;
                _response.Message = "Telefone não encontrado";
                return NotFound(_response);
            }

            if (_currentUserService.IsInRole(SystemRoles.Cliente) && _currentUserService.UserId != telefoneDTO.ClienteId)
                return Forbid();

            if (IsTenantUser() && !await ClientePermiteTelefones(telefoneDTO.ClienteId))
                return Forbid();

            await _telefoneService.Remove(id);

            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = null;
            _response.Message = "Telefone deletado com sucesso";
            return Ok(_response);
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
                await _clienteService.AllowsFieldInOficina(clienteId, oficinaId.Value, ClienteCompartilhamentoCampos.Telefones);
        }

    }
}
