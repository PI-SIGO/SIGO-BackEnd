using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Security;
using SIGO.Services.Interfaces;
using System.Security.Claims;

namespace SIGO.Controllers
{
    [Route("api/pedidos")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.SelfServiceAccess)]
    public class PedidoController : ControllerBase
    {
        private readonly IPedidoService _pedidoService;
        private readonly IServicoService _servicoService;
        private readonly IFuncionarioService _funcionarioService;
        private readonly Response _response;

        public PedidoController(
            IPedidoService pedidoService,
            IServicoService servicoService,
            IFuncionarioService funcionarioService)
        {
            _pedidoService = pedidoService;
            _servicoService = servicoService;
            _funcionarioService = funcionarioService;
            _response = new Response();
        }

        [HttpGet]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Cliente}")]
        public async Task<IActionResult> GetAll()
        {
            var pedidos = await _pedidoService.GetAll();
            if (IsCliente())
            {
                var clienteId = GetCurrentUserId();
                pedidos = pedidos.Where(p => clienteId.HasValue && p.idCliente == clienteId.Value);
            }

            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = pedidos;
            _response.Message = "Pedidos listados com sucesso";

            return Ok(_response);
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Cliente}")]
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

            if (IsCliente() && GetCurrentUserId() != pedido.idCliente)
                return Forbid();

            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = pedido;
            _response.Message = "Pedido encontrado com sucesso";
            return Ok(_response);
        }

        [HttpGet("me/servicos")]
        [Authorize(Roles = SystemRoles.Cliente)]
        public async Task<IActionResult> GetMyServices()
        {
            var clienteId = GetCurrentUserId();
            if (!clienteId.HasValue)
                return Forbid();

            var pedidos = await _pedidoService.GetAll();
            var serviceIds = pedidos
                .Where(p => p.idCliente == clienteId.Value)
                .SelectMany(p => p.Pedido_Servicos)
                .Select(ps => ps.IdServico)
                .Distinct()
                .ToList();

            var services = await _servicoService.GetAll();
            var result = services.Where(s => serviceIds.Contains(s.Id)).ToList();
            return Ok(result);
        }

        [HttpGet("me/funcionarios")]
        [Authorize(Roles = SystemRoles.Cliente)]
        public async Task<IActionResult> GetMyEmployees()
        {
            var clienteId = GetCurrentUserId();
            if (!clienteId.HasValue)
                return Forbid();

            var pedidos = await _pedidoService.GetAll();
            var employeeIds = pedidos
                .Where(p => p.idCliente == clienteId.Value)
                .Select(p => p.idFuncionario)
                .Distinct()
                .ToList();

            var employees = await _funcionarioService.GetAll();
            var result = employees.Where(f => employeeIds.Contains(f.Id)).ToList();
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina}")]
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

        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina}")]
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

        [HttpDelete("{id:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina}")]
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
