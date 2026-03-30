using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Services.Interfaces;

namespace SIGO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CorController : ControllerBase
    {
        private readonly ICorService _corService;
        private readonly Response _response;

        public CorController(ICorService corService, IMapper mapper)
        {
            _corService = corService;
            _response = new Response();
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var cores = await _corService.GetAll();

            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = cores;
            _response.Message = "Cores listadas com sucesso";

            return Ok(_response);
        }

        [HttpGet("name/{nome}")]
        public async Task<IActionResult> GetByName(string nome)
        {
            var cores = await _corService.GetByName(nome);

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
        public async Task<IActionResult> Create(CorDTO corDto)
        {
            await _corService.Create(corDto);
            return Ok(new { Message = "Cor cadastrada com sucesso" });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CorDTO corDto)
        {
            if (corDto == null)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Data = null;
                _response.Message = "Dados inválidos";

                return BadRequest(_response);
            }
            // força o id da URL no DTO (evita mismatch)
            corDto.Id = id;

            try
            {
                await _corService.UpdateCor(corDto, id);
                return Ok(new { Message = "Cor atualizada com sucesso" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { Message = "Cor não encontrada" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _corService.Remove(id);
            return Ok(new { Message = "Cor removida com sucesso" });
        }
    }
}
