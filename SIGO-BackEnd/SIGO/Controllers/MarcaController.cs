using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Services.Interfaces;

namespace SIGO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MarcaController : ControllerBase
    {
        private readonly IMarcaService _marcaService;
        private readonly Response _response;
        private readonly IMapper _mapper;

        public MarcaController(IMarcaService marcaService, IMapper mapper)
        {
            _marcaService = marcaService;
            _mapper = mapper;
            _response = new Response();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var marcasDTO = await _marcaService.GetAll();
            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = marcasDTO;
            _response.Message = "Marcas listadas com sucesso";
            return Ok(_response);
        }

        [HttpGet("id/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var marcaDto = await _marcaService.GetById(id);
            if (marcaDto is null)
                return NotFound(new { Message = "Marca não encontrada" });
            return Ok(marcaDto);
        }

        [HttpGet("name/{nomeMarca}")]
        public async Task<IActionResult> GetByName(string nomeMarca)
        {
            var marcasDto = await _marcaService.GetByName(nomeMarca);
            if (!marcasDto.Any())
                return NotFound(new { Message = "Nenhuma marca encontrada com esse nome" });
            return Ok(marcasDto);
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody]  MarcaDTO marcaDTO)
        {
            await _marcaService.Create(marcaDTO);
            _response.Code = ResponseEnum.SUCCESS;
            _response.Message = "Marca criada com sucesso";
            return Ok(_response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] MarcaDTO marcaDTO)
        {
            await _marcaService.Update(marcaDTO, id);
            _response.Code = ResponseEnum.SUCCESS;
            _response.Message = "Marca atualizada com sucesso";
            return Ok(_response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Remove(int id)
        {
            await _marcaService.Remove(id);
            _response.Code = ResponseEnum.SUCCESS;
            _response.Message = "Marca removida com sucesso";
            return Ok(_response);
        }
    }
}
