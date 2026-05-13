using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Security;
using SIGO.Services.Interfaces;
using SIGO.Validation;

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
        private readonly ICurrentUserService _currentUserService;
        private readonly Response _response;

        public PedidoController(
            IPedidoService pedidoService,
            IServicoService servicoService,
            IFuncionarioService funcionarioService,
            ICurrentUserService currentUserService)
        {
            _pedidoService = pedidoService;
            _servicoService = servicoService;
            _funcionarioService = funcionarioService;
            _currentUserService = currentUserService;
            _response = new Response();
        }

        [HttpGet]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Cliente}")]
        public async Task<IActionResult> GetAll()
        {
            IEnumerable<PedidoDTO> pedidos;
            if (_currentUserService.IsInRole(SystemRoles.Cliente))
            {
                var clienteId = _currentUserService.UserId;
                if (!clienteId.HasValue)
                    return Forbid();

                pedidos = await _pedidoService.GetByCliente(clienteId.Value);
            }
            else if (_currentUserService.IsInRole(SystemRoles.Oficina))
            {
                var oficinaId = _currentUserService.OficinaId;
                if (!oficinaId.HasValue)
                    return Forbid();

                pedidos = await _pedidoService.GetByOficina(oficinaId.Value);
            }
            else
            {
                pedidos = await _pedidoService.GetAll();
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
            PedidoDTO? pedido;

            if (_currentUserService.IsInRole(SystemRoles.Oficina))
            {
                var oficinaId = _currentUserService.OficinaId;
                if (!oficinaId.HasValue)
                    return Forbid();

                pedido = await _pedidoService.GetByIdForOficina(id, oficinaId.Value);
            }
            else
            {
                pedido = await _pedidoService.GetById(id);
            }

            if (pedido is null)
            {
                _response.Code = ResponseEnum.NOT_FOUND;
                _response.Data = null;
                _response.Message = "Pedido não encontrado";
                return NotFound(_response);
            }

            if (_currentUserService.IsInRole(SystemRoles.Cliente) && _currentUserService.UserId != pedido.idCliente)
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
            var clienteId = _currentUserService.UserId;
            if (!clienteId.HasValue)
                return Forbid();

            var pedidos = await _pedidoService.GetByCliente(clienteId.Value);
            var serviceIds = pedidos
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
            var clienteId = _currentUserService.UserId;
            if (!clienteId.HasValue)
                return Forbid();

            var pedidos = await _pedidoService.GetByCliente(clienteId.Value);
            var employeeIds = pedidos
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
                if (_currentUserService.IsInRole(SystemRoles.Oficina))
                {
                    var oficinaId = _currentUserService.OficinaId;
                    if (!oficinaId.HasValue)
                        return Forbid();

                    await _pedidoService.CreateForOficina(pedidoDTO, oficinaId.Value);
                }
                else
                {
                    await _pedidoService.Create(pedidoDTO);
                }

                _response.Code = ResponseEnum.SUCCESS;
                _response.Data = pedidoDTO;
                _response.Message = "Pedido cadastrado com sucesso";
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
                PedidoDTO? existingPedido;
                if (_currentUserService.IsInRole(SystemRoles.Oficina))
                {
                    var oficinaId = _currentUserService.OficinaId;
                    if (!oficinaId.HasValue)
                        return Forbid();

                    existingPedido = await _pedidoService.GetByIdForOficina(id, oficinaId.Value);
                }
                else
                {
                    existingPedido = await _pedidoService.GetById(id);
                }

                if (existingPedido is null)
                {
                    _response.Code = ResponseEnum.NOT_FOUND;
                    _response.Data = null;
                    _response.Message = "Pedido não encontrado";
                    return NotFound(_response);
                }

                if (_currentUserService.IsInRole(SystemRoles.Oficina))
                {
                    await _pedidoService.UpdateForOficina(pedidoDTO, id, existingPedido.idOficina);
                }
                else
                {
                    await _pedidoService.Update(pedidoDTO, id);
                }

                _response.Code = ResponseEnum.SUCCESS;
                _response.Data = pedidoDTO;
                _response.Message = "Pedido atualizado com sucesso";
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
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina}")]
        public async Task<IActionResult> Delete(int id)
        {
            PedidoDTO? existingPedido;
            if (_currentUserService.IsInRole(SystemRoles.Oficina))
            {
                var oficinaId = _currentUserService.OficinaId;
                if (!oficinaId.HasValue)
                    return Forbid();

                existingPedido = await _pedidoService.GetByIdForOficina(id, oficinaId.Value);
            }
            else
            {
                existingPedido = await _pedidoService.GetById(id);
            }

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

    }
}
