using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Security;
using SIGO.Services.Interfaces;
using SIGO.Utils;
using SIGO.Validation;

namespace SIGO.Controllers
{
    [Route("api/funcionarios")]
    [ApiController]
    [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina}")]
    public class FuncionarioController : ControllerBase
    {
        private readonly IFuncionarioService _funcionarioService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IFuncionarioRoleResolver _funcionarioRoleResolver;
        private readonly ICurrentUserService _currentUserService;
        private readonly Response _response;
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
            _response = new Response();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
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

            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = funcionarioDTO;
            _response.Message = "Funcionário listados com sucesso";

            return Ok(_response);
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
                return NotFound(new { Message = "Funcionário não encontrado" });

            return Ok(funcionarioDTO);
        }

        [HttpGet("nome/{nome}")]
        public async Task<IActionResult> GetFuncionarioByNome(string nome)
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

            if (!clientesDto.Any())
                return NotFound(new { Message = "Nenhum funcionário encontrado com esse nome" });

            return Ok(clientesDto);
        }

        [HttpPost]
        public async Task<IActionResult> Post(FuncionarioRequestDTO funcionarioDTO)
        {
            if (funcionarioDTO is null)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Data = null;
                _response.Message = "Dados inválidos";

                return BadRequest(_response);
            }

            try
            {
                funcionarioDTO.Id = 0;
                SanitizeFuncionario(funcionarioDTO);
                if (!_currentUserService.IsInRole(SystemRoles.Admin))
                {
                    var oficinaId = _currentUserService.OficinaId;
                    if (!oficinaId.HasValue)
                        return Forbid();

                    funcionarioDTO.IdOficina = oficinaId.Value;
                    funcionarioDTO.Role = SystemRoles.Funcionario;
                }
                else
                {
                    funcionarioDTO.Role = NormalizeRole(funcionarioDTO.Role);
                }

                await _funcionarioService.Create(funcionarioDTO);

                _response.Code = ResponseEnum.SUCCESS;
                _response.Data = ToResponse(funcionarioDTO);
                _response.Message = "Funcionário cadastrado com sucesso";

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
        public async Task<IActionResult> Put(int id, FuncionarioRequestDTO funcionarioDTO)
        {
            if (funcionarioDTO is null)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Data = null;
                _response.Message = "Dados inválidos";

                return BadRequest(_response);
            }

            try
            {

                FuncionarioDTO? existingFuncionarioDTO;
                if (_currentUserService.IsInRole(SystemRoles.Admin))
                {
                    existingFuncionarioDTO = await _funcionarioService.GetById(id);
                    funcionarioDTO.Role = NormalizeRole(funcionarioDTO.Role);
                }
                else
                {
                    var oficinaId = _currentUserService.OficinaId;
                    if (!oficinaId.HasValue)
                        return Forbid();

                    existingFuncionarioDTO = await _funcionarioService.GetByIdForOficina(id, oficinaId.Value);
                    funcionarioDTO.IdOficina = oficinaId.Value;
                    funcionarioDTO.Role = SystemRoles.Funcionario;
                }

                if (existingFuncionarioDTO is null)
                {
                    _response.Code = ResponseEnum.NOT_FOUND;
                    _response.Data = null;
                    _response.Message = "O funcionário informado não existe";
                    return NotFound(_response);
                }

                SanitizeFuncionario(funcionarioDTO);

                await _funcionarioService.Update(funcionarioDTO, id);

                _response.Code = ResponseEnum.SUCCESS;
                _response.Data = ToResponse(funcionarioDTO);
                _response.Message = "Funcionário atualizado com sucesso";

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
        public async Task<IActionResult> DeleteFuncionario(int id)
        {
            FuncionarioDTO? clienteDTO;
            if (_currentUserService.IsInRole(SystemRoles.Admin))
            {
                clienteDTO = await _funcionarioService.GetById(id);
            }
            else
            {
                var oficinaId = _currentUserService.OficinaId;
                if (!oficinaId.HasValue)
                    return Forbid();

                clienteDTO = await _funcionarioService.GetByIdForOficina(id, oficinaId.Value);
            }

            if (clienteDTO is null)
            {
                _response.Code = ResponseEnum.NOT_FOUND;
                _response.Data = null;
                _response.Message = "Funcionário não encontrado";
                return NotFound(_response);
            }
            await _funcionarioService.Remove(id);

            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = null;
            _response.Message = "Funcionário deletado com sucesso";
            return Ok(_response);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting(RateLimitPolicies.FuncionarioLogin)]
        public async Task<ActionResult> Login([FromBody] Login login)
        {
            if (login is null)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Data = null;
                _response.Message = "Dados inválidos";

                return BadRequest(_response);
            }

            var funcionarioDTO = await _funcionarioService.Login(login);

            if (funcionarioDTO is null)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Data = null;
                _response.Message = "Email ou senha incorretos";

                return BadRequest(_response);
            }

            var role = _funcionarioRoleResolver.Resolve(funcionarioDTO.Role);
            if (role == SystemRoles.Funcionario && !funcionarioDTO.IdOficina.HasValue)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Data = null;
                _response.Message = "Funcionário sem oficina vinculada";
                return BadRequest(_response);
            }

            var token = _jwtTokenService.GenerateToken(new JwtTokenRequest
            {
                UserId = funcionarioDTO.Id,
                Name = funcionarioDTO.Nome,
                Email = funcionarioDTO.Email,
                Role = role,
                OficinaId = funcionarioDTO.IdOficina
            });
            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = token;
            _response.Message = "Login realizado com sucesso";

            return Ok(_response);
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
