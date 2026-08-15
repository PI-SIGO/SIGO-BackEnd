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
    [Route("api/v1/servicos")]
    [Authorize(Policy = AuthorizationPolicies.OperationalAccess)]
    public sealed class ServicoController : ControllerBase
    {
        private readonly IServicoService _servicoService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditoriaFuncionarioService _auditoriaService;

        public ServicoController(
            IServicoService servicoService,
            ICurrentUserService currentUserService,
            IAuditoriaFuncionarioService auditoriaService)
        {
            _servicoService = servicoService;
            _currentUserService = currentUserService;
            _auditoriaService = auditoriaService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResponse<ServicoDTO>>> GetAll(
            [FromQuery] PaginationRequest? pagination = null)
        {
            pagination ??= new PaginationRequest();

            var services = _currentUserService.IsInRole(SystemRoles.Admin)
                ? await _servicoService.GetAll()
                : await _servicoService.GetByOficina(RequireOfficeId());

            return Ok(
                PagedResponse<ServicoDTO>.Create(
                    services,
                    pagination));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ServicoDTO>> GetByIdWithDetails(int id)
        {
            var service = _currentUserService.IsInRole(SystemRoles.Admin)
                ? await _servicoService.GetByIdWithDetails(id)
                : await _servicoService.GetByIdWithDetailsForOficina(
                    id,
                    RequireOfficeId());

            return service is null
                ? this.ApiProblem(
                    StatusCodes.Status404NotFound,
                    "Serviço não encontrado.")
                : Ok(service);
        }

        [HttpGet("nome/{nome}")]
        public async Task<ActionResult<PagedResponse<ServicoDTO>>> GetByNameWithDetails(
            string nome,
            [FromQuery] PaginationRequest? pagination = null)
        {
            pagination ??= new PaginationRequest();

            var services = _currentUserService.IsInRole(SystemRoles.Admin)
                ? await _servicoService.GetByNameWithDetails(nome)
                : await _servicoService.GetByNameWithDetailsForOficina(
                    nome,
                    RequireOfficeId());

            return Ok(
                PagedResponse<ServicoDTO>.Create(
                    services,
                    pagination));
        }

        [HttpPost]
        public async Task<ActionResult<ServicoDTO>> Post(
            [FromBody] ServicoDTO request)
        {
            if (_currentUserService.IsInRole(SystemRoles.Admin))
            {
                await _servicoService.Create(request);
            }
            else
            {
                await _servicoService.CreateForOficina(
                    request,
                    RequireOfficeId());
            }

            await _auditoriaService.Registrar(
                "CADASTROU",
                "Servico",
                request.Id > 0 ? request.Id : null,
                request.Id > 0
                    ? $"Cadastrou o serviço #{request.Id}."
                    : "Cadastrou um novo serviço.");

            return Created(
                $"/api/v1/servicos/{request.Id}",
                request);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ServicoDTO>> Put(
            int id,
            [FromBody] ServicoDTO request)
        {
            if (_currentUserService.IsInRole(SystemRoles.Admin))
            {
                await _servicoService.Update(
                    request,
                    id);
            }
            else
            {
                await _servicoService.UpdateForOficina(
                    request,
                    id,
                    RequireOfficeId());
            }

            request.Id = id;

            await _auditoriaService.Registrar(
                "ALTEROU",
                "Servico",
                id,
                $"Alterou os dados do serviço #{id}.");

            return Ok(request);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var service = _currentUserService.IsInRole(SystemRoles.Admin)
                ? await _servicoService.GetById(id)
                : await _servicoService.GetByIdForOficina(
                    id,
                    RequireOfficeId());

            if (service is null)
            {
                return this.ApiProblem(
                    StatusCodes.Status404NotFound,
                    "Serviço não encontrado.");
            }

            await _servicoService.Remove(id);

            await _auditoriaService.Registrar(
                "EXCLUIU",
                "Servico",
                id,
                $"Excluiu o serviço #{id}.");

            return NoContent();
        }

        private int RequireOfficeId() =>
            _currentUserService.OficinaId
            ?? throw new UnauthorizedAccessException(
                "Token nao contem oficina_id.");
    }
}