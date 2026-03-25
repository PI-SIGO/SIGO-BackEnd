using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Services.Interfaces;

namespace SIGO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VeiculoController : ControllerBase
    {
        private readonly IVeiculoService _veiculoService;
        private readonly Response _response;

        public VeiculoController(IVeiculoService veiculoService, IMapper mapper)
        {
            _veiculoService = veiculoService;
            _response = new Response();
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var veiculos = await _veiculoService.GetAll();

            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = veiculos;
            _response.Message = "Veículos listados com sucesso";

            return Ok(_response);
        }

        [HttpGet("placa/{placa}")]
        public async Task<IActionResult> GetByPlaca(string placa)
        {
            // Busca por placas que contenham a string fornecida
            var veiculos = await _veiculoService.GetByPlaca(placa);

            if (!veiculos.Any())
            {
                _response.Code = ResponseEnum.NOT_FOUND;
                _response.Data = null;
                _response.Message = "Nenhum veículo encontrado com essa placa";
                return NotFound(_response);
            }

            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = veiculos;
            _response.Message = "Veículos encontrados com sucesso";
            return Ok(_response);
        }

        [HttpGet("tipo/{tipo}")]
        public async Task<IActionResult> GetByTipo(string tipo)
        {
            // Busca por tipos que contenham a string fornecida
            var veiculos = await _veiculoService.GetByTipo(tipo);

            if (!veiculos.Any())
            {
                _response.Code = ResponseEnum.NOT_FOUND;
                _response.Data = null;
                _response.Message = "Nenhum veículo encontrado com esse tipo";
                return NotFound(_response);
            }

            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = veiculos;
            _response.Message = "Veículos encontrados com sucesso";
            return Ok(_response);
        }

        [HttpPost]
        public async Task<IActionResult> Create(VeiculoDTO veiculoDto)
        {
            await _veiculoService.Create(veiculoDto);
            return Ok(new { Message = "Veículo cadastrado com sucesso" });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, VeiculoDTO veiculoDto)
        {
            try
            {
                await _veiculoService.UpdateVeiculo(veiculoDto, id);
                return Ok(new { Message = "Veículo atualizado com sucesso" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _veiculoService.Remove(id);
            return Ok(new { Message = "Veículo removido com sucesso" });
        }
    }
}
