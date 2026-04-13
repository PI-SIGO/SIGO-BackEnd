using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Security;
using SIGO.Services.Interfaces;
using SIGO.Utils;
using System.Security.Claims;

namespace SIGO.Controllers
{
    [Route("api/telefones")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.SelfServiceAccess)]
    public class TelefoneController : ControllerBase
    {
        private readonly ITelefoneService _telefoneService;
        private readonly Response _response;
        private readonly IMapper _mapper;

        public TelefoneController(ITelefoneService telefoneService, IMapper mapper)
        {
            _telefoneService = telefoneService;
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

            if (IsCliente() && GetCurrentUserId() != telefoneDto.ClienteId)
                return Forbid();

            return Ok(telefoneDto);
        }

        [HttpGet("nome/{nome}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario}")]
        public async Task<IActionResult> GetByNameWithDetails(string nome)
        {
            var clientesDto = await _telefoneService.GetTelefoneByNome(nome);

            if (!clientesDto.Any())
                return NotFound(new { Message = "Nenhum cliente encontrado com esse nome" });

            return Ok(clientesDto);
        }

        [HttpPost]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario},{SystemRoles.Cliente}")]
        public async Task<IActionResult> Post(TelefoneDTO telefoneDTO)
        {
            if (telefoneDTO is null)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Data = null;
                _response.Message = "Dados inválidos";

                return BadRequest(_response);
            }

            try
            {
                telefoneDTO.Id = 0;
                SanitizeTelefone(telefoneDTO);

                if (IsCliente())
                {
                    var clienteId = GetCurrentUserId();
                    if (!clienteId.HasValue || telefoneDTO.ClienteId != clienteId.Value)
                        return Forbid();
                }

                await _telefoneService.Create(telefoneDTO);

                _response.Code = ResponseEnum.SUCCESS;
                _response.Data = telefoneDTO;
                _response.Message = "Telefone cadastrado com sucesso";

                return Ok(_response);
            }
            catch (Exception)
            {
                _response.Code = ResponseEnum.ERROR;
                _response.Message = "Não foi possível cadastrar o telefone";
                _response.Data = null;
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario},{SystemRoles.Cliente}")]
        public async Task<IActionResult> Put(int id, TelefoneDTO telefoneDTO)
        {
            if (telefoneDTO is null)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Data = null;
                _response.Message = "Dados inválidos";

                return BadRequest(_response);
            }

            try
            {
                var existingTelefoneDTO = await _telefoneService.GetById(id);
                if (existingTelefoneDTO is null)
                {
                    _response.Code = ResponseEnum.NOT_FOUND;
                    _response.Data = null;
                    _response.Message = "O telefone informado não existe";
                    return NotFound(_response);
                }

                if (IsCliente() && GetCurrentUserId() != existingTelefoneDTO.ClienteId)
                    return Forbid();

                SanitizeTelefone(telefoneDTO);

                if (IsCliente())
                {
                    var clienteId = GetCurrentUserId();
                    if (!clienteId.HasValue || telefoneDTO.ClienteId != clienteId.Value)
                        return Forbid();
                }

                await _telefoneService.Update(telefoneDTO, id);

                _response.Code = ResponseEnum.SUCCESS;
                _response.Data = telefoneDTO;
                _response.Message = "Telefone atualizado com sucesso";

                return Ok(_response);
            }
            catch (Exception)
            {
                _response.Code = ResponseEnum.ERROR;
                _response.Message = "Ocorreu um erro ao tentar atualizar os dados do telefone";
                _response.Data = null;
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario},{SystemRoles.Cliente}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var telefoneDTO = await _telefoneService.GetById(id);

                if (telefoneDTO is null)
                {
                    _response.Code = ResponseEnum.NOT_FOUND;
                    _response.Data = null;
                    _response.Message = "Telefone não encontrado";
                    return NotFound(_response);
                }

                if (IsCliente() && GetCurrentUserId() != telefoneDTO.ClienteId)
                    return Forbid();

                await _telefoneService.Remove(id);

                _response.Code = ResponseEnum.SUCCESS;
                _response.Data = null;
                _response.Message = "Telefone deletado com sucesso";
                return Ok(_response);
            }
            catch (Exception)
            {
                _response.Code = ResponseEnum.ERROR;
                _response.Message = "Ocorreu um erro ao deletar o telefone";
                _response.Data = null;
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        private static void SanitizeTelefone(TelefoneDTO telefoneDTO)
        {
            telefoneDTO.Numero = SanitizeHelper.ApenasDigitos(telefoneDTO.Numero);
        }

        private bool IsCliente()
        {
            return User.IsInRole(SystemRoles.Cliente);
        }

        private int? GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var id) ? id : null;
        }
    }
}
