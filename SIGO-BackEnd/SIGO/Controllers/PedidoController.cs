using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGO.Errors;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Security;
using SIGO.Services.Interfaces;

namespace SIGO.Controllers
{
    [ApiController]
    [Route("api/v1/pedidos")]
    [Authorize(Policy = AuthorizationPolicies.SelfServiceAccess)]
    public sealed class PedidoController : ControllerBase
    {
        private readonly IPedidoService _pedidoService;
        private readonly IServicoService _servicoService;
        private readonly IFuncionarioService _funcionarioService;
        private readonly ICurrentUserService _currentUserService;

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
        }

        [HttpGet]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario},{SystemRoles.Cliente}")]
        public async Task<ActionResult<PagedResponse<PedidoDTO>>> GetAll(
            [FromQuery] PaginationRequest? pagination = null)
        {
            pagination ??= new PaginationRequest();
            IEnumerable<PedidoDTO> orders;
            if (_currentUserService.IsInRole(SystemRoles.Cliente))
            {
                orders = await _pedidoService.GetByCliente(RequireUserId());
            }
            else if (IsOfficeScopedActor())
            {
                orders = await _pedidoService.GetByOficina(RequireOfficeId());
            }
            else
            {
                orders = await _pedidoService.GetAll();
            }

            return Ok(PagedResponse<PedidoDTO>.Create(orders, pagination));
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario},{SystemRoles.Cliente}")]
        public async Task<ActionResult<PedidoDTO>> GetById(int id)
        {
            var order = IsOfficeScopedActor()
                ? await _pedidoService.GetByIdForOficina(id, RequireOfficeId())
                : await _pedidoService.GetById(id);

            if (order is null)
                return this.ApiProblem(
                    StatusCodes.Status404NotFound,
                    "Pedido não encontrado.");
            if (_currentUserService.IsInRole(SystemRoles.Cliente) && order.idCliente != RequireUserId())
                return Forbid();

            return Ok(order);
        }

        [HttpGet("me/servicos")]
        [Authorize(Roles = SystemRoles.Cliente)]
        public async Task<ActionResult<PagedResponse<ServicoDTO>>> GetMyServices(
            [FromQuery] PaginationRequest? pagination = null)
        {
            pagination ??= new PaginationRequest();
            var orders = await _pedidoService.GetByCliente(RequireUserId());
            var serviceIds = orders
                .SelectMany(order => order.Pedido_Servicos)
                .Select(item => item.IdServico)
                .ToHashSet();
            var services = (await _servicoService.GetAll())
                .Where(service => serviceIds.Contains(service.Id));
            return Ok(PagedResponse<ServicoDTO>.Create(services, pagination));
        }

        [HttpGet("me/funcionarios")]
        [Authorize(Roles = SystemRoles.Cliente)]
        public async Task<ActionResult<PagedResponse<FuncionarioDTO>>> GetMyEmployees(
            [FromQuery] PaginationRequest? pagination = null)
        {
            pagination ??= new PaginationRequest();
            var orders = await _pedidoService.GetByCliente(RequireUserId());
            var employeeIds = orders.Select(order => order.idFuncionario).ToHashSet();
            var employees = (await _funcionarioService.GetAll())
                .Where(employee => employeeIds.Contains(employee.Id));
            return Ok(PagedResponse<FuncionarioDTO>.Create(employees, pagination));
        }

        [HttpPost]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario}")]
        public async Task<ActionResult<PedidoDTO>> Post([FromBody] PedidoDTO request)
        {
            if (IsOfficeScopedActor())
                await _pedidoService.CreateForOficina(request, RequireOfficeId());
            else
                await _pedidoService.Create(request);

            return Created($"/api/v1/pedidos/{request.Id}", request);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario}")]
        public async Task<ActionResult<PedidoDTO>> Put(int id, [FromBody] PedidoDTO request)
        {
            if (IsOfficeScopedActor())
                await _pedidoService.UpdateForOficina(request, id, RequireOfficeId());
            else
                await _pedidoService.Update(request, id);

            return Ok(request);
        }

        [HttpPatch("{id:int}/status")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario}")]
        [ProducesResponseType(typeof(PedidoDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<PedidoDTO>> UpdateStatus(
            int id,
            [FromBody] AtualizarStatusRequestDTO request,
            CancellationToken cancellationToken = default)
        {
            if (request?.Status is not SIGO.Objects.Enums.Status status)
            {
                return this.ApiValidationProblem(
                    StatusCodes.Status422UnprocessableEntity,
                    nameof(request.Status),
                    "O status e obrigatorio.");
            }

            var updated = IsOfficeScopedActor()
                ? await _pedidoService.UpdateStatusForOficina(
                    id,
                    status,
                    RequireOfficeId(),
                    cancellationToken)
                : await _pedidoService.UpdateStatus(id, status, cancellationToken);

            return Ok(updated);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = _currentUserService.IsInRole(SystemRoles.Oficina)
                ? await _pedidoService.GetByIdForOficina(id, RequireOfficeId())
                : await _pedidoService.GetById(id);
            if (existing is null)
                return this.ApiProblem(
                    StatusCodes.Status404NotFound,
                    "Pedido não encontrado.");

            await _pedidoService.Remove(id);
            return NoContent();
        }

        private int RequireOfficeId() =>
            _currentUserService.OficinaId
            ?? throw new UnauthorizedAccessException("Token nao contem oficina_id.");

        private bool IsOfficeScopedActor() =>
            _currentUserService.IsInRole(SystemRoles.Oficina) ||
            _currentUserService.IsInRole(SystemRoles.Funcionario);

        private int RequireUserId() =>
            _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Token nao contem identificador do usuario.");
    }
}
