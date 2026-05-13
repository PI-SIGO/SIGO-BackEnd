using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Security;
using SIGO.Services.Interfaces;
using SIGO.Utils;
using SIGO.Validation;

namespace SIGO.Controllers
{
    [Route("api/oficinas")]
    [ApiController]
    [Authorize]
    public class OficinaController : ControllerBase
    {
        private readonly IOficinaService _oficinaService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ICurrentUserService _currentUserService;
        private readonly Response _response;

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
            _response = new Response();
        }

        [HttpGet]
        [Authorize(Roles = SystemRoles.Admin)]
        public async Task<IActionResult> Get()
        {
            var cores = await _oficinaService.GetAll();

            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = cores;
            _response.Message = "Cores listadas com sucesso";

            return Ok(_response);
        }

        [HttpGet("nome/{nome}")]
        [Authorize(Roles = SystemRoles.Admin)]
        public async Task<IActionResult> GetByName(string nome)
        {
            var cores = await _oficinaService.GetByName(nome);

            if (!cores.Any())
            {
                _response.Code = ResponseEnum.NOT_FOUND;
                _response.Data = null;
                _response.Message = "Nenhuma cor encontrada";
                return NotFound(_response);
            }

            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = cores;
            _response.Message = "Cores encontradas com sucesso";
            return Ok(_response);
        }

        [HttpPost]
        [AllowAnonymous]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting(RateLimitPolicies.PublicRegistration)]
        public async Task<IActionResult> Create(OficinaRequestDTO oficinaDto)
        {
            try
            {
                SanitizeOficina(oficinaDto);

                await _oficinaService.Create(oficinaDto);
                return Ok(new { Message = "Oficina cadastrada com sucesso" });
            }
            catch (BusinessValidationException ex)
            {
                return BadRequest(new { Message = "Dados inválidos", Errors = ex.Errors });
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina}")]
        public async Task<IActionResult> Update(int id, [FromBody] OficinaRequestDTO oficinaDto)
        {
            if (oficinaDto == null)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Data = null;
                _response.Message = "Dados inválidos";

                return BadRequest(_response);
            }

            try
            {
                SanitizeOficina(oficinaDto);

                if (_currentUserService.IsInRole(SystemRoles.Oficina))
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
                    oficinaDto.Id = id;
                    await _oficinaService.Update(oficinaDto, id);
                }

                return Ok(new { Message = "Oficina atualizada com sucesso" });
            }
            catch (BusinessValidationException ex)
            {
                return BadRequest(new { Message = "Dados inválidos", Errors = ex.Errors });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { Message = "Oficina não encontrada" });
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = SystemRoles.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _oficinaService.Remove(id);
                return Ok(new { Message = "Oficina removida com sucesso" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { Message = "Oficina não encontrada" });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting(RateLimitPolicies.OficinaLogin)]
        public async Task<ActionResult> Login([FromBody] Login login)
        {
            if (login is null)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Data = null;
                _response.Message = "Dados inválidos";

                return BadRequest(_response);
            }

            var oficinaDTO = await _oficinaService.Login(login);

            if (oficinaDTO is null)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Data = null;
                _response.Message = "Email ou senha incorretos";

                return BadRequest(_response);
            }

            var token = _jwtTokenService.GenerateToken(new JwtTokenRequest
            {
                UserId = oficinaDTO.Id,
                Name = oficinaDTO.Nome,
                Email = oficinaDTO.Email,
                Role = SystemRoles.Oficina,
                OficinaId = oficinaDTO.Id
            });
            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = token;
            _response.Message = "Login realizado com sucesso";

            return Ok(_response);
        }

        private static void SanitizeOficina(OficinaRequestDTO oficinaDTO)
        {
            oficinaDTO.CNPJ = SanitizeHelper.ApenasDigitos(oficinaDTO.CNPJ);
        }
    }
}
