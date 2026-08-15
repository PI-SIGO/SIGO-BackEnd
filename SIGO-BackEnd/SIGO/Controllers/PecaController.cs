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
    [Route("api/v1/pecas")]
    [Authorize(Policy = AuthorizationPolicies.OperationalAccess)]
    public sealed class PecaController : ControllerBase
    {
        private readonly IPecaService _pecaService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditoriaFuncionarioService _auditoriaService;

        public PecaController(
            IPecaService pecaService,
            ICurrentUserService currentUserService,
            IAuditoriaFuncionarioService auditoriaService)
        {
            _pecaService = pecaService;
            _currentUserService = currentUserService;
            _auditoriaService = auditoriaService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResponse<PecaDTO>>> GetAll(
            [FromQuery] PaginationRequest? pagination = null)
        {
            pagination ??= new PaginationRequest();

            var pieces = _currentUserService.IsInRole(SystemRoles.Admin)
                ? await _pecaService.GetAll()
                : await _pecaService.GetByOficina(RequireOfficeId());

            return Ok(
                PagedResponse<PecaDTO>.Create(
                    pieces,
                    pagination));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<PecaDTO>> Get(int id)
        {
            var piece = _currentUserService.IsInRole(SystemRoles.Admin)
                ? await _pecaService.GetById(id)
                : await _pecaService.GetByIdForOficina(
                    id,
                    RequireOfficeId());

            return piece is null
                ? this.ApiProblem(
                    StatusCodes.Status404NotFound,
                    "Peça não encontrada.")
                : Ok(piece);
        }

        [HttpPost]
        public async Task<ActionResult<PecaDTO>> Post(
            [FromBody] PecaDTO request)
        {
            if (_currentUserService.IsInRole(SystemRoles.Admin))
            {
                await _pecaService.Create(request);
            }
            else
            {
                await _pecaService.CreateForOficina(
                    request,
                    RequireOfficeId());
            }

            await _auditoriaService.Registrar(
                "CADASTROU",
                "Peca",
                request.Id > 0 ? request.Id : null,
                request.Id > 0
                    ? $"Cadastrou a peça #{request.Id}."
                    : "Cadastrou uma nova peça.");

            return Created(
                $"/api/v1/pecas/{request.Id}",
                request);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<PecaDTO>> Put(
            int id,
            [FromBody] PecaDTO request)
        {
            if (_currentUserService.IsInRole(SystemRoles.Admin))
            {
                await _pecaService.Update(
                    request,
                    id);
            }
            else
            {
                await _pecaService.UpdateForOficina(
                    request,
                    id,
                    RequireOfficeId());
            }

            request.Id = id;

            await _auditoriaService.Registrar(
                "ALTEROU",
                "Peca",
                id,
                $"Alterou os dados da peça #{id}.");

            return Ok(request);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var piece = _currentUserService.IsInRole(SystemRoles.Admin)
                ? await _pecaService.GetById(id)
                : await _pecaService.GetByIdForOficina(
                    id,
                    RequireOfficeId());

            if (piece is null)
            {
                return this.ApiProblem(
                    StatusCodes.Status404NotFound,
                    "Peça não encontrada.");
            }

            await _pecaService.Remove(id);

            await _auditoriaService.Registrar(
                "EXCLUIU",
                "Peca",
                id,
                $"Excluiu a peça #{id}.");

            return NoContent();
        }

        private int RequireOfficeId() =>
            _currentUserService.OficinaId
            ?? throw new UnauthorizedAccessException(
                "Token nao contem oficina_id.");
    }
}