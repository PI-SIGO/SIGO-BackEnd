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
    [Route("api/v1/funcionarios")]
    [ApiController]
    [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario}")]
    public class FuncionarioController : ControllerBase
    {
        private readonly IFuncionarioService _funcionarioService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IFuncionarioRoleResolver _funcionarioRoleResolver;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public FuncionarioController(
            IFuncionarioService funcionarioService,
            IMapper mapper,
            IPasswordHasher passwordHasher,
            IJwtTokenService jwtTokenService,
            IFuncionarioRoleResolver funcionarioRoleResolver,
            ICurrentUserService currentUserService)
        {
            _funcionarioService = funcionarioService;
            _mapper = mapper;
            _jwtTokenService = jwtTokenService;
            _funcionarioRoleResolver = funcionarioRoleResolver;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResponse<FuncionarioDTO>>> GetAll(
            [FromQuery] PaginationRequest? pagination = null)
        {
            IEnumerable<FuncionarioDTO> funcionarioDTO;
            if (_currentUserService.IsInRole(SystemRoles.Admin))
            {
                funcionarioDTO = await _funcionarioService.GetAll();
            }
            else
            {
                var oficinaId = _currentUserService.OficinaId;
                if (!oficinaId.HasValue)
                    return Forbid();

                funcionarioDTO = await _funcionarioService.GetByOficina(oficinaId.Value);
            }

            return Ok(PagedResponse<FuncionarioDTO>.Create(
                funcionarioDTO,
                pagination ?? new PaginationRequest()));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetFuncionarioById(int id)
        {
            FuncionarioDTO? funcionarioDTO;
            if (_currentUserService.IsInRole(SystemRoles.Admin))
            {
                funcionarioDTO = await _funcionarioService.GetById(id);
            }
            else
            {
                var oficinaId = _currentUserService.OficinaId;
                if (!oficinaId.HasValue)
                    return Forbid();

                funcionarioDTO = await _funcionarioService.GetByIdForOficina(id, oficinaId.Value);
            }

            if (funcionarioDTO is null)
                return this.ApiProblem(
                    StatusCodes.Status404NotFound,
                    "Funcionário não encontrado.");

            return Ok(funcionarioDTO);
        }

        [HttpGet("nome/{nome}")]
        public async Task<ActionResult<PagedResponse<FuncionarioDTO>>> GetFuncionarioByNome(
            string nome,
            [FromQuery] PaginationRequest? pagination = null)
        {
            IEnumerable<FuncionarioDTO> clientesDto;
            if (_currentUserService.IsInRole(SystemRoles.Admin))
            {
                clientesDto = await _funcionarioService.GetFuncionarioByNome(nome);
            }
            else
            {
                var oficinaId = _currentUserService.OficinaId;
                if (!oficinaId.HasValue)
                    return Forbid();

                clientesDto = await _funcionarioService.GetFuncionarioByNomeForOficina(nome, oficinaId.Value);
            }

            return Ok(PagedResponse<FuncionarioDTO>.Create(
                clientesDto,
                pagination ?? new PaginationRequest()));
        }

        [HttpPost]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina}")]
        public async Task<IActionResult> Post(FuncionarioRequestDTO funcionarioDTO)
        {
            if (funcionarioDTO is null)
            {
                return this.ApiValidationProblem(
                    StatusCodes.Status400BadRequest,
                    "request",
                    "O corpo da requisição é obrigatório.");
            }

            funcionarioDTO.Id = 0;
            SanitizeFuncionario(funcionarioDTO);
            if (_currentUserService.IsInRole(SystemRoles.Admin))
            {
                funcionarioDTO.Role = NormalizeRole(funcionarioDTO.Role);
            }
            else if (_currentUserService.IsInRole(SystemRoles.Oficina))
            {
                var oficinaId = _currentUserService.OficinaId;
                if (!oficinaId.HasValue)
                    return Forbid();

                funcionarioDTO.IdOficina = oficinaId.Value;
                funcionarioDTO.Role = SystemRoles.Funcionario;
            }
            else
            {
                return Forbid();
            }

            await _funcionarioService.Create(funcionarioDTO);

            var created = ToResponse(funcionarioDTO);
            return Created($"/api/v1/funcionarios/{created.Id}", created);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina}")]
        public async Task<IActionResult> Put(int id, FuncionarioRequestDTO funcionarioDTO)
        {
            if (funcionarioDTO is null)
            {
                return this.ApiValidationProblem(
                    StatusCodes.Status400BadRequest,
                    "request",
                    "O corpo da requisição é obrigatório.");
            }

            funcionarioDTO.Id = id;
            FuncionarioDTO? existingFuncionarioDTO;
            if (_currentUserService.IsInRole(SystemRoles.Admin))
            {
                existingFuncionarioDTO = await _funcionarioService.GetById(id);
                funcionarioDTO.Role = NormalizeRole(funcionarioDTO.Role);
            }
            else if (_currentUserService.IsInRole(SystemRoles.Oficina))
            {
                var oficinaId = _currentUserService.OficinaId;
                if (!oficinaId.HasValue)
                    return Forbid();

                existingFuncionarioDTO = await _funcionarioService.GetByIdForOficina(id, oficinaId.Value);
                funcionarioDTO.IdOficina = oficinaId.Value;
                funcionarioDTO.Role = SystemRoles.Funcionario;
            }
            else
            {
                return Forbid();
            }

            if (existingFuncionarioDTO is null)
            {
                return this.ApiProblem(
                    StatusCodes.Status404NotFound,
                    "O funcionário informado não existe.");
            }

            SanitizeFuncionario(funcionarioDTO);

            await _funcionarioService.Update(funcionarioDTO, id);

            return Ok(ToResponse(funcionarioDTO));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina}")]
        public async Task<IActionResult> DeleteFuncionario(
            int id,
            CancellationToken cancellationToken = default)
        {
            FuncionarioDTO? funcionarioDTO;
            if (_currentUserService.IsInRole(SystemRoles.Admin))
            {
                funcionarioDTO = await _funcionarioService.GetById(id);
            }
            else if (_currentUserService.IsInRole(SystemRoles.Oficina))
            {
                var oficinaId = _currentUserService.OficinaId;
                if (!oficinaId.HasValue)
                    return Forbid();

                funcionarioDTO = await _funcionarioService.GetByIdForOficina(id, oficinaId.Value);
            }
            else
            {
                return Forbid();
            }

            if (funcionarioDTO is null)
            {
                return this.ApiProblem(
                    StatusCodes.Status404NotFound,
                    "Funcionário não encontrado.");
            }
            await _funcionarioService.DeactivateAsync(id, cancellationToken);

            return NoContent();
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting(RateLimitPolicies.FuncionarioLogin)]
        public async Task<ActionResult> Login([FromBody] Login login)
        {
            if (login is null)
            {
                return this.ApiValidationProblem(
                    StatusCodes.Status400BadRequest,
                    "request",
                    "O corpo da requisição é obrigatório.");
            }

            var funcionarioDTO = await _funcionarioService.Login(login);

            if (funcionarioDTO is null)
            {
                return this.ApiProblem(
                    StatusCodes.Status401Unauthorized,
                    "E-mail ou senha inválidos.");
            }

            var role = _funcionarioRoleResolver.Resolve(funcionarioDTO.Role);
            if (role == SystemRoles.Funcionario && !funcionarioDTO.IdOficina.HasValue)
            {
                return this.ApiProblem(
                    StatusCodes.Status401Unauthorized,
                    "Funcionário sem oficina ativa associada.");
            }

            var token = _jwtTokenService.GenerateToken(new JwtTokenRequest
            {
                UserId = funcionarioDTO.Id,
                Name = funcionarioDTO.Nome,
                Email = funcionarioDTO.Email,
                Role = role,
                OficinaId = funcionarioDTO.IdOficina
            });
            return Ok(new AccessTokenResponse(token));
        }

        private static void SanitizeFuncionario(FuncionarioRequestDTO funcionarioDTO)
        {
            funcionarioDTO.Cpf = SanitizeHelper.ApenasDigitos(funcionarioDTO.Cpf);
        }

        private static string NormalizeRole(string? role)
        {
            return string.Equals(role, SystemRoles.Admin, StringComparison.OrdinalIgnoreCase)
                ? SystemRoles.Admin
                : SystemRoles.Funcionario;
        }

        private static FuncionarioDTO ToResponse(FuncionarioDTO funcionarioDTO)
        {
            return new FuncionarioDTO
            {
                Id = funcionarioDTO.Id,
                Nome = funcionarioDTO.Nome,
                Cpf = funcionarioDTO.Cpf,
                Cargo = funcionarioDTO.Cargo,
                Email = funcionarioDTO.Email,
                Situacao = funcionarioDTO.Situacao,
                IdOficina = funcionarioDTO.IdOficina,
                Role = funcionarioDTO.Role
            };
        }
    }
}
