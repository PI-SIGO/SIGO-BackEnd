using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Security;
using SIGO.Services.Interfaces;
using SIGO.Utils;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;

namespace SIGO.Controllers
{
    [Route("api/oficinas")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.FullAccess)]
    public class OficinaController : ControllerBase
    {
        private readonly IOficinaService _oficinaService;
        private readonly Response _response;
        private readonly IConfiguration _configuration;

        public OficinaController(IOficinaService oficinaService, IMapper mapper, IConfiguration configuration)
        {
            _oficinaService = oficinaService;
            _response = new Response();
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var cores = await _oficinaService.GetAll();

            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = cores;
            _response.Message = "Cores listadas com sucesso";

            return Ok(_response);
        }

        [HttpGet("nome/{nome}")]
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
        public async Task<IActionResult> Create(OficinaDTO oficinaDto)
        {
            try
            {
                SanitizeOficina(oficinaDto);
                await _oficinaService.ValidarCnpj(oficinaDto.CNPJ);

                // hash da senha antes de salvar
                oficinaDto.Senha = GenerateSha256Hash(oficinaDto.Senha);

                await _oficinaService.Create(oficinaDto);
                return Ok(new { Message = "Oficina cadastrada com sucesso" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] OficinaDTO oficinaDto)
        {
            if (oficinaDto == null)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Data = null;
                _response.Message = "Dados inválidos";

                return BadRequest(_response);
            }
            // força o id da URL no DTO (evita mismatch)
            oficinaDto.Id = id;

            try
            {
                SanitizeOficina(oficinaDto);
                await _oficinaService.ValidarCnpj(oficinaDto.CNPJ, id);
                await _oficinaService.Update(oficinaDto, id);
                return Ok(new { Message = "Oficina atualizada com sucesso" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { Message = "Oficina não encontrada" });
            }
        }

        [HttpDelete("{id:int}")]
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

        private static string GenerateSha256Hash(string input)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));

            StringBuilder builder = new();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }

        private string GenerateJwtToken(OficinaDTO oficinaDTO)
        {
            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])
            );

            var credentials = new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256
            );

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, oficinaDTO.Id.ToString()),
                new Claim(ClaimTypes.Name, oficinaDTO.Nome),
                new Claim(ClaimTypes.Email, oficinaDTO.Email),
                new Claim(ClaimTypes.Role, SystemRoles.Oficina),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult> Login([FromBody] Login login)
        {
            if (login is null)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Data = null;
                _response.Message = "Dados inválidos";

                return BadRequest(_response);
            }

            try
            {
                login.Password = GenerateSha256Hash(login.Password);
                var oficinaDTO = await _oficinaService.Login(login);

                if (oficinaDTO is null)
                {
                    _response.Code = ResponseEnum.INVALID;
                    _response.Data = null;
                    _response.Message = "Email ou senha incorretos";

                    return BadRequest(_response);
                }

                var token = GenerateJwtToken(oficinaDTO);
                _response.Code = ResponseEnum.SUCCESS;
                _response.Data = token;
                _response.Message = "Login realizado com sucesso";

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.Code = ResponseEnum.ERROR;
                _response.Message = "Não foi possível realizar o login";
                _response.Data = new
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace ?? "No stack trace available"
                };

                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        private static void SanitizeOficina(OficinaDTO oficinaDTO)
        {
            oficinaDTO.CNPJ = SanitizeHelper.ApenasDigitos(oficinaDTO.CNPJ);
        }
    }
}
