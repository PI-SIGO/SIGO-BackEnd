using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Services.Interfaces;

namespace SIGO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PedidoController : ControllerBase
    {
        private readonly IPedidoService _pedidoService;
        private readonly Response _response;

        public PedidoController(IPedidoService pedidoService)
        {
            _pedidoService = pedidoService;
            _response = new Response();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var pedidos = await _pedidoService.GetAll();

            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = pedidos;
            _response.Message = "Pedidos listados com sucesso";

            return Ok(_response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var pedido = await _pedidoService.GetById(id);

            if (pedido is null)
            {
                _response.Code = ResponseEnum.NOT_FOUND;
                _response.Data = null;
                _response.Message = "Pedido não encontrado";
                return NotFound(_response);
            }

            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = pedido;
            _response.Message = "Pedido encontrado com sucesso";
            return Ok(_response);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] PedidoDTO pedidoDTO)
        {
            if (pedidoDTO is null)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Data = null;
                _response.Message = "Dados inválidos";
                return BadRequest(_response);
            }

            try
            {
                pedidoDTO.Id = 0;
                await _pedidoService.Create(pedidoDTO);

                _response.Code = ResponseEnum.SUCCESS;
                _response.Data = pedidoDTO;
                _response.Message = "Pedido cadastrado com sucesso";
                return Ok(_response);
            }
            catch (Exception)
            {
                _response.Code = ResponseEnum.ERROR;
                _response.Message = "Não foi possível cadastrar o pedido";
                _response.Data = null;
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        [HttpPut("id/{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] PedidoDTO pedidoDTO)
        {
            if (pedidoDTO is null)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Data = null;
                _response.Message = "Dados inválidos";
                return BadRequest(_response);
            }

            try
            {
                var existingPedido = await _pedidoService.GetById(id);
                if (existingPedido is null)
                {
                    _response.Code = ResponseEnum.NOT_FOUND;
                    _response.Data = null;
                    _response.Message = "Pedido não encontrado";
                    return NotFound(_response);
                }

                await _pedidoService.Update(pedidoDTO, id);

                _response.Code = ResponseEnum.SUCCESS;
                _response.Data = pedidoDTO;
                _response.Message = "Pedido atualizado com sucesso";
                return Ok(_response);
            }
            catch (Exception)
            {
                _response.Code = ResponseEnum.ERROR;
                _response.Message = "Ocorreu um erro ao atualizar o pedido";
                _response.Data = null;
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var existingPedido = await _pedidoService.GetById(id);
                if (existingPedido is null)
                {
                    _response.Code = ResponseEnum.NOT_FOUND;
                    _response.Data = null;
                    _response.Message = "Pedido não encontrado";
                    return NotFound(_response);
                }

                await _pedidoService.Remove(id);

                _response.Code = ResponseEnum.SUCCESS;
                _response.Data = null;
                _response.Message = "Pedido removido com sucesso";
                return Ok(_response);
            }
            catch (Exception)
            {
                _response.Code = ResponseEnum.ERROR;
                _response.Message = "Ocorreu um erro ao deletar o pedido";
                _response.Data = null;
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }
    }
}
