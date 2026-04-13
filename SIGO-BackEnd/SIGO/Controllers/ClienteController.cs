using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SIGO.Objects.Contracts;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using SIGO.Objects.Dtos.Entities;
using SIGO.Services.Entities;
using SIGO.Services.Interfaces;
using SIGO.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SIGO.Utils;

namespace SIGO.Controllers
{
    [Route("api/clientes")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.SelfServiceAccess)]
    public class ClienteController : ControllerBase
    {
        private readonly IClienteService _clienteService;
        private readonly IConfiguration _configuration;
        private readonly Response _response;
        private readonly IMapper _mapper;

        public ClienteController(IClienteService clienteService, IMapper mapper, IConfiguration configuration)
        {
            _clienteService = clienteService;
            _mapper = mapper;
            _response = new Response();
            _configuration = configuration;
        }

        [HttpGet]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario}")]
        public async Task<IActionResult> GetAll()
        {
            var clienteDTO = await _clienteService.GetAll();

            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = clienteDTO;
            _response.Message = "Clientes listados com sucesso";

            return Ok(_response);
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario},{SystemRoles.Cliente}")]
        public async Task<IActionResult> GetByIdWithDetails(int id)
        {
            if (IsCliente() && GetCurrentUserId() != id)
                return Forbid();

            var clienteDto = await _clienteService.GetByIdWithDetails(id);

            if (clienteDto is null)
                return NotFound(new { Message = "Cliente não encontrado" });

            return Ok(clienteDto);
        }

        [HttpGet("nome/{nome}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario}")]
        public async Task<IActionResult> GetByNameWithDetails(string nome)
        {
            var clientesDto = await _clienteService.GetByNameWithDetails(nome);

            if (!clientesDto.Any())
                return NotFound(new { Message = "Nenhum cliente encontrado com esse nome" });

            return Ok(clientesDto);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Post(ClienteDTO clienteDTO)
        {
            if (clienteDTO is null)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Data = null;
                _response.Message = "Dados inválidos";

                return BadRequest(_response);
            }

            try
            {
                clienteDTO.Id = 0;
                SanitizeCliente(clienteDTO);
                await _clienteService.ValidarCpfCnpj(clienteDTO.Cpf_Cnpj);

                clienteDTO.senha = GenerateSha256Hash(clienteDTO.senha);
                await _clienteService.Create(clienteDTO);

                _response.Code = ResponseEnum.SUCCESS;
                _response.Data = clienteDTO;
                _response.Message = "Cliente cadastrado com sucesso";

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
                _response.Message = "Não foi possível cadastrar o cliente";
                _response.Data = null;
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario},{SystemRoles.Cliente}")]
        public async Task<IActionResult> Put([FromRoute] int id, [FromBody] ClienteDTO clienteDTO)
        {
            if (IsCliente() && GetCurrentUserId() != id)
                return Forbid();

            if (clienteDTO is null)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Data = null;
                _response.Message = "Dados inválidos";

                return BadRequest(_response);
            }

            try
            {
                var existingClienteDTO = await _clienteService.GetById(id);
                if (existingClienteDTO is null)
                {
                    _response.Code = ResponseEnum.NOT_FOUND;
                    _response.Data = null;
                    _response.Message = "O cliente informado não existe";
                    return NotFound(_response);
                }

                SanitizeCliente(clienteDTO);

                await _clienteService.ValidarCpfCnpj(clienteDTO.Cpf_Cnpj, id);
                await _clienteService.Update(clienteDTO, id);

                _response.Code = ResponseEnum.SUCCESS;
                _response.Data = clienteDTO;
                _response.Message = "Cliente atualizado com sucesso";

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
                _response.Message = "Ocorreu um erro ao tentar atualizar os dados do cliente";
                _response.Data = null;
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Cliente}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (IsCliente() && GetCurrentUserId() != id)
                return Forbid();

            try
            {
                var clienteDTO = await _clienteService.GetById(id);

                if (clienteDTO is null)
                {
                    _response.Code = ResponseEnum.NOT_FOUND;
                    _response.Data = null;
                    _response.Message = "Cliente não encontrado";
                    return NotFound(_response);
                }

                await _clienteService.Remove(id);

                _response.Code = ResponseEnum.SUCCESS;
                _response.Data = null;
                _response.Message = "Cliente deletado com sucesso";
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
            // Converte a string de entrada para um array de bytes e computa o hash
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));

            // Converte o array de bytes para uma string hexadecimal
            StringBuilder builder = new();
            for (int i = 0; i < bytes.Length; i++)
            {
                // Formata cada byte como dois dígitos hexadecimais
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }

        private string GenerateJwtToken(ClienteDTO clienteDTO)
        {
            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])
            );

            var credentials = new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256
            );

            // Claims são informações sobre o usuário que você quer armazenar no token
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, clienteDTO.Id.ToString()),
                new Claim(ClaimTypes.Name, clienteDTO.Nome),
                new Claim(ClaimTypes.Email, clienteDTO.Email),
                new Claim(ClaimTypes.Role, SystemRoles.Cliente),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(2), // Define que o token expira em 2 horas
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
                var professorDTO = await _clienteService.Login(login);

                if (professorDTO is null)
                {
                    _response.Code = ResponseEnum.INVALID;
                    _response.Data = null;
                    _response.Message = "Email ou senha incorretos";

                    return BadRequest(_response);
                }

                var token = GenerateJwtToken(professorDTO);
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

        private static void SanitizeCliente(ClienteDTO clienteDTO)
        {
            clienteDTO.Cpf_Cnpj = SanitizeHelper.ApenasDigitos(clienteDTO.Cpf_Cnpj);
            clienteDTO.Cep = SanitizeHelper.ApenasDigitos(clienteDTO.Cep);

            if (clienteDTO.Telefones == null)
                return;

            foreach (var telefone in clienteDTO.Telefones)
            {
                telefone.Numero = SanitizeHelper.ApenasDigitos(telefone.Numero);
            }
        }

        private bool IsCliente()
        {
            return User.IsInRole(SystemRoles.Cliente);
        }

        private int? GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var id) ? id : null;
        }
    }
}
