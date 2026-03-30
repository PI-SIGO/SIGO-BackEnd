using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Services.Interfaces;

namespace SIGO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PecaController : ControllerBase
    {
        private readonly IPecaService _pecaService;
        private readonly Response _response;
        private readonly IMapper _mapper;

        public PecaController(IPecaService pecaService, IMapper mapper)
        {
            _pecaService = pecaService;
            _mapper = mapper;
            _response = new Response();
        }

        [HttpGet("id/{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var clienteDto = await _pecaService.GetById(id);

            if (clienteDto is null)
                return NotFound(new { Message = "Peça não encontrada" });

            return Ok(clienteDto);
        }


        [HttpPost]
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

                await _pecaService.Create(pecaDTO);

                _response.Code = ResponseEnum.SUCCESS;
                _response.Data = pecaDTO;
                _response.Message = "Peça cadastrada com sucesso";

                return Ok(_response);
            }
            catch (Exception)
            {
                _response.Code = ResponseEnum.ERROR;
                _response.Message = "Não foi possível cadastrar a peça";
                _response.Data = null;
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        [HttpPut("{id}")]
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
                var existingClienteDTO = await _pecaService.GetById(id);
                if (existingClienteDTO is null)
                {
                    _response.Code = ResponseEnum.NOT_FOUND;
                    _response.Data = null;
                    _response.Message = "A peça informada não existe";
                    return NotFound(_response);
                }

                await _pecaService.Update(pecaDTO, id);

                _response.Code = ResponseEnum.SUCCESS;
                _response.Data = pecaDTO;
                _response.Message = "Peça atualizada com sucesso";

                return Ok(_response);
            }
            catch (Exception)
            {
                _response.Code = ResponseEnum.ERROR;
                _response.Message = "Ocorreu um erro ao tentar atualizar os dados da peça";
                _response.Data = null;
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var clienteDTO = await _pecaService.GetById(id);

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
            catch (Exception)
            {
                _response.Code = ResponseEnum.ERROR;
                _response.Message = "Ocorreu um erro ao deletar a peça";
                _response.Data = null;
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }
    }
}
