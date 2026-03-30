using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Services.Interfaces;
using SIGO.Utils;

namespace SIGO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OficinaController : ControllerBase
    {
        private readonly IOficinaService _oficinaService;
        private readonly Response _response;

        public OficinaController(IOficinaService oficinaService, IMapper mapper)
        {
            _oficinaService = oficinaService;
            _response = new Response();
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var cores = await _oficinaService.GetAll();

            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = cores;
            _response.Message = "Cores listadas com sucesso";

            return Ok(_response);
        }

        [HttpGet("name/{nome}")]
        public async Task<IActionResult> GetByName(string nome)
        {
            var cores = await _oficinaService.GetByName(nome);

            if (!cores.Any())
            {
                _response.Code = ResponseEnum.NOT_FOUND;
                _response.Data = null;
                _response.Message = "Nenhuma cor encontrada";
                return NotFound(_response);
            }

            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = cores;
            _response.Message = "Cores encontradas com sucesso";
            return Ok(_response);
        }

        [HttpPost]
        public async Task<IActionResult> Create(OficinaDTO oficinaDto)
        {
            try
            {
                SanitizeOficina(oficinaDto);
                await _oficinaService.ValidarCnpj(oficinaDto.CNPJ);
                await _oficinaService.Create(oficinaDto);
                return Ok(new { Message = "Cor cadastrada com sucesso" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("id/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] OficinaDTO oficinaDto)
        {
            if (oficinaDto == null)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Data = null;
                _response.Message = "Dados inválidos";

                return BadRequest(_response);
            }
            // força o id da URL no DTO (evita mismatch)
            oficinaDto.Id = id;

            try
            {
                SanitizeOficina(oficinaDto);
                await _oficinaService.ValidarCnpj(oficinaDto.CNPJ, id);
                await _oficinaService.Update(oficinaDto, id);
                return Ok(new { Message = "Cor atualizada com sucesso" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { Message = "Cor não encontrada" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _oficinaService.Remove(id);
                return Ok(new { Message = "Cor removida com sucesso" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { Message = "Cor não encontrada" });
            }
        }

        private static void SanitizeOficina(OficinaDTO oficinaDTO)
        {
            oficinaDTO.CNPJ = SanitizeHelper.ApenasDigitos(oficinaDTO.CNPJ);
        }
    }
}
