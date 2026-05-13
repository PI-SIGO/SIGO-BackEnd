using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Security;
using SIGO.Services.Entities;
using SIGO.Services.Interfaces;
using SIGO.Validation;

namespace SIGO.Controllers
{
    [Route("api/servicos")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(Policy = AuthorizationPolicies.OperationalAccess)]
    public class ServicoController : ControllerBase
    {
        private readonly IServicoService _servicoService;
        private readonly ICurrentUserService _currentUserService;
        private readonly Response _response;
        private readonly IMapper _mapper;

        public ServicoController(IServicoService servicoService, IMapper mapper, ICurrentUserService currentUserService)
        {
            _servicoService = servicoService;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _response = new Response();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            IEnumerable<ServicoDTO> servicoDTO;
            if (_currentUserService.IsInRole(SystemRoles.Admin))
            {
                servicoDTO = await _servicoService.GetAll();
            }
            else
            {
                var oficinaId = _currentUserService.OficinaId;
                if (!oficinaId.HasValue)
                    return Forbid();

                servicoDTO = await _servicoService.GetByOficina(oficinaId.Value);
            }

            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = servicoDTO;
            _response.Message = "Serviços listados com sucesso";

            return Ok(_response);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetByIdWithDetails(int id)
        {
            ServicoDTO? servicoDto;
            if (_currentUserService.IsInRole(SystemRoles.Admin))
            {
                servicoDto = await _servicoService.GetByIdWithDetails(id);
            }
            else
            {
                var oficinaId = _currentUserService.OficinaId;
                if (!oficinaId.HasValue)
                    return Forbid();

                servicoDto = await _servicoService.GetByIdWithDetailsForOficina(id, oficinaId.Value);
            }

            if (servicoDto is null)
                return NotFound(new { Message = "Serviço não encontrado" });

            return Ok(servicoDto);
        }

        [HttpGet("nome/{nome}")]
        public async Task<IActionResult> GetByNameWithDetails(string nome)
        {
            IEnumerable<ServicoDTO> servicoDto;
            if (_currentUserService.IsInRole(SystemRoles.Admin))
            {
                servicoDto = await _servicoService.GetByNameWithDetails(nome);
            }
            else
            {
                var oficinaId = _currentUserService.OficinaId;
                if (!oficinaId.HasValue)
                    return Forbid();

                servicoDto = await _servicoService.GetByNameWithDetailsForOficina(nome, oficinaId.Value);
            }

            if (!servicoDto.Any())
                return NotFound(new { Message = "Nenhum serviço encontrado com esse nome" });

            return Ok(servicoDto);
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina}")]
        public async Task<IActionResult> Post(ServicoDTO serviceDTO)
        {
            if (serviceDTO is null)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Data = null;
                _response.Message = "Dados inválidos";

                return BadRequest(_response);
            }

            try
            {
                serviceDTO.Id = 0;

                if (_currentUserService.IsInRole(SystemRoles.Admin))
                {
                    await _servicoService.Create(serviceDTO);
                }
                else
                {
                    var oficinaId = _currentUserService.OficinaId;
                    if (!oficinaId.HasValue)
                        return Forbid();

                    await _servicoService.CreateForOficina(serviceDTO, oficinaId.Value);
                }

                _response.Code = ResponseEnum.SUCCESS;
                _response.Data = serviceDTO;
                _response.Message = "Serviço cadastrado com sucesso";

                return Ok(_response);
            }
            catch (BusinessValidationException ex)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Message = "Dados inválidos";
                _response.Data = ex.Errors;
                return BadRequest(_response);
            }
        }

        [HttpPut("{id:int}")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina}")]
        public async Task<IActionResult> Put(int id, ServicoDTO servicoDTO)
        {
            if (servicoDTO is null)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Data = null;
                _response.Message = "Dados inválidos";

                return BadRequest(_response);
            }

            try
            {
                servicoDTO.Id = id;

                ServicoDTO? existingServiceDTO;
                if (_currentUserService.IsInRole(SystemRoles.Admin))
                {
                    existingServiceDTO = await _servicoService.GetById(id);
                }
                else
                {
                    var oficinaId = _currentUserService.OficinaId;
                    if (!oficinaId.HasValue)
                        return Forbid();

                    existingServiceDTO = await _servicoService.GetByIdForOficina(id, oficinaId.Value);
                }

                if (existingServiceDTO is null)
                {
                    _response.Code = ResponseEnum.NOT_FOUND;
                    _response.Data = null;
                    _response.Message = "O serviço informado não existe";
                    return NotFound(_response);
                }

                if (_currentUserService.IsInRole(SystemRoles.Admin))
                {
                    await _servicoService.Update(servicoDTO, id);
                }
                else
                {
                    var oficinaId = _currentUserService.OficinaId!.Value;
                    await _servicoService.UpdateForOficina(servicoDTO, id, oficinaId);
                }

                _response.Code = ResponseEnum.SUCCESS;
                _response.Data = servicoDTO;
                _response.Message = "Serviço atualizado com sucesso";

                return Ok(_response);
            }
            catch (BusinessValidationException ex)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Message = "Dados inválidos";
                _response.Data = ex.Errors;
                return BadRequest(_response);
            }
        }

        [HttpDelete("{id:int}")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina}")]
        public async Task<IActionResult> Delete(int id)
        {
            ServicoDTO? servicoDTO;
            if (_currentUserService.IsInRole(SystemRoles.Admin))
            {
                servicoDTO = await _servicoService.GetById(id);
            }
            else
            {
                var oficinaId = _currentUserService.OficinaId;
                if (!oficinaId.HasValue)
                    return Forbid();

                servicoDTO = await _servicoService.GetByIdForOficina(id, oficinaId.Value);
            }

            if (servicoDTO is null)
            {
                _response.Code = ResponseEnum.NOT_FOUND;
                _response.Data = null;
                _response.Message = "Serviço não encontrado";
                return NotFound(_response);
            }

            await _servicoService.Remove(id);

            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = null;
            _response.Message = "Serviço deletado com sucesso";
            return Ok(_response);
        }
    }
}
