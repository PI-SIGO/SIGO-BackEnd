using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Security;
using SIGO.Services.Interfaces;
using SIGO.Validation;

namespace SIGO.Controllers
{
    [Route("api/pecas")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(Policy = AuthorizationPolicies.OperationalAccess)]
    public class PecaController : ControllerBase
    {
        private readonly IPecaService _pecaService;
        private readonly ICurrentUserService _currentUserService;
        private readonly Response _response;
        private readonly IMapper _mapper;

        public PecaController(IPecaService pecaService, IMapper mapper, ICurrentUserService currentUserService)
        {
            _pecaService = pecaService;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _response = new Response();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            IEnumerable<PecaDTO> pecasDTO;
            if (_currentUserService.IsInRole(SystemRoles.Admin))
            {
                pecasDTO = await _pecaService.GetAll();
            }
            else
            {
                var oficinaId = _currentUserService.OficinaId;
                if (!oficinaId.HasValue)
                    return Forbid();

                pecasDTO = await _pecaService.GetByOficina(oficinaId.Value);
            }

            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = pecasDTO;
            _response.Message = "Pecas listadas com sucesso";
            return Ok(_response);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            PecaDTO? clienteDto;
            if (_currentUserService.IsInRole(SystemRoles.Admin))
            {
                clienteDto = await _pecaService.GetById(id);
            }
            else
            {
                var oficinaId = _currentUserService.OficinaId;
                if (!oficinaId.HasValue)
                    return Forbid();

                clienteDto = await _pecaService.GetByIdForOficina(id, oficinaId.Value);
            }

            if (clienteDto is null)
                return NotFound(new { Message = "Peça não encontrada" });

            return Ok(clienteDto);
        }


        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina}")]
        public async Task<IActionResult> Post(PecaDTO pecaDTO)
        {
            if (pecaDTO is null)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Data = null;
                _response.Message = "Dados inválidos";

                return BadRequest(_response);
            }

            try
            {
                pecaDTO.Id = 0;

                if (_currentUserService.IsInRole(SystemRoles.Admin))
                {
                    await _pecaService.Create(pecaDTO);
                }
                else
                {
                    var oficinaId = _currentUserService.OficinaId;
                    if (!oficinaId.HasValue)
                        return Forbid();

                    await _pecaService.CreateForOficina(pecaDTO, oficinaId.Value);
                }

                _response.Code = ResponseEnum.SUCCESS;
                _response.Data = pecaDTO;
                _response.Message = "Peça cadastrada com sucesso";

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
        public async Task<IActionResult> Put(int id, PecaDTO pecaDTO)
        {
            if (pecaDTO is null)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Data = null;
                _response.Message = "Dados inválidos";

                return BadRequest(_response);
            }

            try
            {
                PecaDTO? existingClienteDTO;
                if (_currentUserService.IsInRole(SystemRoles.Admin))
                {
                    existingClienteDTO = await _pecaService.GetById(id);
                }
                else
                {
                    var oficinaId = _currentUserService.OficinaId;
                    if (!oficinaId.HasValue)
                        return Forbid();

                    existingClienteDTO = await _pecaService.GetByIdForOficina(id, oficinaId.Value);
                }

                if (existingClienteDTO is null)
                {
                    _response.Code = ResponseEnum.NOT_FOUND;
                    _response.Data = null;
                    _response.Message = "A peça informada não existe";
                    return NotFound(_response);
                }

                if (_currentUserService.IsInRole(SystemRoles.Admin))
                {
                    await _pecaService.Update(pecaDTO, id);
                }
                else
                {
                    var oficinaId = _currentUserService.OficinaId!.Value;
                    await _pecaService.UpdateForOficina(pecaDTO, id, oficinaId);
                }

                _response.Code = ResponseEnum.SUCCESS;
                _response.Data = pecaDTO;
                _response.Message = "Peça atualizada com sucesso";

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
            PecaDTO? clienteDTO;
            if (_currentUserService.IsInRole(SystemRoles.Admin))
            {
                clienteDTO = await _pecaService.GetById(id);
            }
            else
            {
                var oficinaId = _currentUserService.OficinaId;
                if (!oficinaId.HasValue)
                    return Forbid();

                clienteDTO = await _pecaService.GetByIdForOficina(id, oficinaId.Value);
            }

            if (clienteDTO is null)
            {
                _response.Code = ResponseEnum.NOT_FOUND;
                _response.Data = null;
                _response.Message = "Peça não encontrada";
                return NotFound(_response);
            }

            await _pecaService.Remove(id);

            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = null;
            _response.Message = "Peça deletada com sucesso";
            return Ok(_response);
        }
    }
}
