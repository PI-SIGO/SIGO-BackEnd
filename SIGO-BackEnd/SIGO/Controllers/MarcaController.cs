using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGO.Errors;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Security;
using SIGO.Services.Interfaces;

namespace SIGO.Controllers
{
    [Route("api/v1/marcas")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.SelfServiceAccess)]
    public class MarcaController : ControllerBase
    {
        private readonly IMarcaService _marcaService;
        private readonly IMapper _mapper;

        public MarcaController(IMarcaService marcaService, IMapper mapper)
        {
            _marcaService = marcaService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PaginationRequest? pagination = null)
        {
            var marcasDTO = await _marcaService.GetAll();
            return Ok(PagedResponse<MarcaDTO>.Create(marcasDTO, pagination ?? new PaginationRequest()));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var marcaDto = await _marcaService.GetById(id);
            if (marcaDto is null)
                return this.ApiProblem(
                    StatusCodes.Status404NotFound,
                    "Marca não encontrada.");
            return Ok(marcaDto);
        }

        [HttpGet("nome/{nomeMarca}")]
        public async Task<IActionResult> GetByName(
            string nomeMarca,
            [FromQuery] PaginationRequest? pagination = null)
        {
            var marcasDto = await _marcaService.GetByName(nomeMarca);
            return Ok(PagedResponse<MarcaDTO>.Create(marcasDto, pagination ?? new PaginationRequest()));
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.FullAccess)]
        public async Task<IActionResult> Add([FromBody]  MarcaDTO marcaDTO)
        {
            var marcaCriada = await _marcaService.CreateMarca(marcaDTO);
            return Created($"/api/v1/marcas/{marcaCriada.Id}", marcaCriada);
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = AuthorizationPolicies.FullAccess)]
        public async Task<IActionResult> Update(int id, [FromBody] MarcaDTO marcaDTO)
        {
            var marcaAtualizada = await _marcaService.UpdateMarca(marcaDTO, id);
            return Ok(marcaAtualizada);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = AuthorizationPolicies.FullAccess)]
        public async Task<IActionResult> Remove(int id)
        {
            await _marcaService.Remove(id);
            return NoContent();
        }
    }
}
