using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Services.Entities;
using SIGO.Services.Interfaces;
using SIGO.Utils;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;

namespace SIGO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FuncionarioController : ControllerBase
    {
        private readonly IFuncionarioService _funcionarioService;
        private readonly Response _response;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public FuncionarioController(IFuncionarioService funcionarioService, IMapper mapper, IConfiguration configuration)
        {
            _funcionarioService = funcionarioService;
            _mapper = mapper;
            _response = new Response();
            _configuration = configuration;
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetAll()
        {
            var funcionarioDTO = await _funcionarioService.GetAll();

            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = funcionarioDTO;
            _response.Message = "Funcionário listados com sucesso";

            return Ok(_response);
        }

        [HttpGet("id/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetFuncionarioById(int id)
        {
            var funcionarioDTO = await _funcionarioService.GetById(id);

            if (funcionarioDTO is null)
                return NotFound(new { Message = "Funcionário não encontrado" });

            return Ok(funcionarioDTO);
        }

        [HttpGet("name/{nome}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetFuncionarioByNome(string nome)
        {
            var clientesDto = await _funcionarioService.GetFuncionarioByNome(nome);

            if (!clientesDto.Any())
                return NotFound(new { Message = "Nenhum funcionário encontrado com esse nome" });

            return Ok(clientesDto);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Post(FuncionarioDTO funcionarioDTO)
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
                await _funcionarioService.ValidarCpf(funcionarioDTO.Cpf);

                // hash da senha antes de salvar
                funcionarioDTO.Senha = GenerateSha256Hash(funcionarioDTO.Senha);

                await _funcionarioService.Create(funcionarioDTO);

                _response.Code = ResponseEnum.SUCCESS;
                _response.Data = funcionarioDTO;
                _response.Message = "Funcionário cadastrado com sucesso";

                return Ok(_response);
            }
            catch (ArgumentException ex)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Message = ex.Message;
                _response.Data = null;
                return BadRequest(_response);
            }
            catch (Exception)
            {
                _response.Code = ResponseEnum.ERROR;
                _response.Message = "Não foi possível cadastrar o funcionário";
                _response.Data = null;
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        [HttpPut("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Put(int id, FuncionarioDTO funcionarioDTO)
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

                var existingFuncionarioDTO = await _funcionarioService.GetById(id);
                if (existingFuncionarioDTO is null)
                {
                    _response.Code = ResponseEnum.NOT_FOUND;
                    _response.Data = null;
                    _response.Message = "O funcionário informado não existe";
                    return NotFound(_response);
                }

                SanitizeFuncionario(funcionarioDTO);

                await _funcionarioService.ValidarCpf(funcionarioDTO.Cpf, id);

                await _funcionarioService.Update(funcionarioDTO, id);

                _response.Code = ResponseEnum.SUCCESS;
                _response.Data = funcionarioDTO;
                _response.Message = "Funcionário atualizado com sucesso";

                return Ok(_response);
            }
            catch (ArgumentException ex)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Message = ex.Message;
                _response.Data = null;
                return BadRequest(_response);
            }
            catch (Exception)
            {
                _response.Code = ResponseEnum.ERROR;
                _response.Message = "Ocorreu um erro ao tentar atualizar os dados do funcionário";
                _response.Data = null;
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> DeleteFuncionario(int id)
        {
            try
            {
                var clienteDTO = await _funcionarioService.GetById(id);

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
            catch (Exception)
            {
                _response.Code = ResponseEnum.ERROR;
                _response.Message = "Ocorreu um erro ao deletar o cliente";
                _response.Data = null;
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
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

        private string GenerateJwtToken(FuncionarioDTO funcionarioDTO)
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
                new Claim(JwtRegisteredClaimNames.Sub, funcionarioDTO.Nome),
                new Claim(JwtRegisteredClaimNames.Email, funcionarioDTO.Email),
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

        [HttpPost("Login")]
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
                var funcionarioDTO = await _funcionarioService.Login(login);

                if (funcionarioDTO is null)
                {
                    _response.Code = ResponseEnum.INVALID;
                    _response.Data = null;
                    _response.Message = "Email ou senha incorretos";

                    return BadRequest(_response);
                }

                var token = GenerateJwtToken(funcionarioDTO);
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

        private static void SanitizeFuncionario(FuncionarioDTO funcionarioDTO)
        {
            funcionarioDTO.Cpf = SanitizeHelper.ApenasDigitos(funcionarioDTO.Cpf);
        }
    }
}
