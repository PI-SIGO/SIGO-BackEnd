using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SIGO.Errors;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Security;
using SIGO.Services.Interfaces;
using SIGO.Utils;

namespace SIGO.Controllers
{
    [Route("api/v1/oficinas")]
    [ApiController]
    [Authorize]
    public class OficinaController : ControllerBase
    {
        private readonly IOficinaService _oficinaService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ICurrentUserService _currentUserService;

        public OficinaController(
            IOficinaService oficinaService,
            IMapper mapper,
            IPasswordHasher passwordHasher,
            IJwtTokenService jwtTokenService,
            ICurrentUserService currentUserService)
        {
            _oficinaService = oficinaService;
            _jwtTokenService = jwtTokenService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        [Authorize(Roles = SystemRoles.Admin)]
        public async Task<ActionResult<PagedResponse<OficinaDTO>>> Get(
            [FromQuery] PaginationRequest? pagination = null)
        {
            var oficinas = await _oficinaService.GetAll();
            return Ok(PagedResponse<OficinaDTO>.Create(
                oficinas,
                pagination ?? new PaginationRequest()));
        }

        [HttpGet("nome/{nome}")]
        [Authorize(Roles = SystemRoles.Admin)]
        public async Task<ActionResult<PagedResponse<OficinaDTO>>> GetByName(
            string nome,
            [FromQuery] PaginationRequest? pagination = null)
        {
            var oficinas = await _oficinaService.GetByName(nome);
            return Ok(PagedResponse<OficinaDTO>.Create(
                oficinas,
                pagination ?? new PaginationRequest()));
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles =
            $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario}")]
        public async Task<ActionResult<OficinaDTO>> GetById(int id)
        {
            if (!_currentUserService.IsInRole(SystemRoles.Admin))
            {
                var currentOficinaId = _currentUserService.OficinaId;

                if (!currentOficinaId.HasValue ||
                    currentOficinaId.Value != id)
                {
                    return Forbid();
                }
            }

            var oficina = await _oficinaService.GetById(id);

            return oficina is null
                ? this.ApiProblem(
                    StatusCodes.Status404NotFound,
                    "Oficina não encontrada.")
                : Ok(oficina);
        }

        [HttpPost]
        [AllowAnonymous]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting(RateLimitPolicies.PublicRegistration)]
        public async Task<IActionResult> Create(OficinaRequestDTO oficinaDto)
        {
            SanitizeOficina(oficinaDto);

            await _oficinaService.Create(oficinaDto);
            var created = await _oficinaService.GetById(oficinaDto.Id)
                ?? throw new KeyNotFoundException("Oficina cadastrada não foi encontrada.");
            return Created($"/api/v1/oficinas/{created.Id}", created);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina}")]
        public async Task<IActionResult> Update(int id, [FromBody] OficinaRequestDTO oficinaDto)
        {
            if (oficinaDto == null)
            {
                return this.ApiValidationProblem(
                    StatusCodes.Status400BadRequest,
                    "request",
                    "O corpo da requisição é obrigatório.");
            }

            SanitizeOficina(oficinaDto);

            if (_currentUserService.IsInRole(SystemRoles.Admin))
            {
                oficinaDto.Id = id;
                await _oficinaService.Update(oficinaDto, id);
            }
            else if (_currentUserService.IsInRole(SystemRoles.Oficina))
            {
                var oficinaId = _currentUserService.OficinaId;
                if (!oficinaId.HasValue)
                    return Forbid();

                if (id != oficinaId.Value)
                    return Forbid();

                oficinaDto.Id = oficinaId.Value;
                await _oficinaService.UpdateSelfProfile(oficinaDto, oficinaId.Value);
            }
            else
            {
                return Forbid();
            }

            var updated = await _oficinaService.GetById(id)
                ?? throw new KeyNotFoundException("Oficina atualizada não foi encontrada.");
            return Ok(updated);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina}")]
        public async Task<IActionResult> Delete(
            int id,
            CancellationToken cancellationToken = default)
        {
            if (!_currentUserService.IsInRole(SystemRoles.Admin))
            {
                if (!_currentUserService.IsInRole(SystemRoles.Oficina))
                    return Forbid();

                var oficinaId = _currentUserService.OficinaId;
                if (!oficinaId.HasValue || oficinaId.Value != id)
                    return Forbid();
            }

            await _oficinaService.DeactivateAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting(RateLimitPolicies.OficinaLogin)]
        public async Task<ActionResult> Login([FromBody] Login login)
        {
            if (login is null)
            {
                return this.ApiValidationProblem(
                    StatusCodes.Status400BadRequest,
                    "request",
                    "O corpo da requisição é obrigatório.");
            }

            var oficinaDTO = await _oficinaService.Login(login);

            if (oficinaDTO is null)
            {
                return this.ApiProblem(
                    StatusCodes.Status401Unauthorized,
                    "E-mail ou senha inválidos.");
            }

            var token = _jwtTokenService.GenerateToken(new JwtTokenRequest
            {
                UserId = oficinaDTO.Id,
                Name = oficinaDTO.Nome,
                Email = oficinaDTO.Email,
                Role = SystemRoles.Oficina,
                OficinaId = oficinaDTO.Id
            });
            return Ok(new AccessTokenResponse(token));
        }

        private static void SanitizeOficina(OficinaRequestDTO oficinaDTO)
        {
            oficinaDTO.CNPJ = SanitizeHelper.ApenasDigitos(oficinaDTO.CNPJ);
        }
    }
}
