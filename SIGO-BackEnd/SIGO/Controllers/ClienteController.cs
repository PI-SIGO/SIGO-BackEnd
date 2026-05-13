using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Services.Interfaces;
using SIGO.Security;
using SIGO.Utils;
using SIGO.Validation;

namespace SIGO.Controllers
{
    [Route("api/clientes")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.SelfServiceAccess)]
    public class ClienteController : ControllerBase
    {
        private readonly IClienteService _clienteService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ICurrentUserService _currentUserService;
        private readonly Response _response;
        private readonly IMapper _mapper;

        public ClienteController(
            IClienteService clienteService,
            IMapper mapper,
            IPasswordHasher passwordHasher,
            IJwtTokenService jwtTokenService,
            ICurrentUserService currentUserService)
        {
            _clienteService = clienteService;
            _mapper = mapper;
            _passwordHasher = passwordHasher;
            _jwtTokenService = jwtTokenService;
            _currentUserService = currentUserService;
            _response = new Response();
        }

        [HttpGet]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario}")]
        public async Task<IActionResult> GetAll()
        {
            if (_currentUserService.IsInRole(SystemRoles.Admin))
            {
                var clienteDTO = await _clienteService.GetAll();

                _response.Code = ResponseEnum.SUCCESS;
                _response.Data = clienteDTO;
                _response.Message = "Clientes listados com sucesso";

                return Ok(_response);
            }

            var oficinaId = _currentUserService.OficinaId;
            if (!oficinaId.HasValue)
                return Forbid();

            var clientesOficina = await _clienteService.GetByOficina(oficinaId.Value);

            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = clientesOficina;
            _response.Message = "Clientes listados com sucesso";

            return Ok(_response);
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario},{SystemRoles.Cliente}")]
        public async Task<IActionResult> GetByIdWithDetails(int id)
        {
            if (_currentUserService.IsInRole(SystemRoles.Cliente) && _currentUserService.UserId != id)
                return Forbid();

            if (_currentUserService.IsInRole(SystemRoles.Admin) || _currentUserService.IsInRole(SystemRoles.Cliente))
            {
                var clienteDto = await _clienteService.GetByIdWithDetails(id);
                if (clienteDto is null)
                    return NotFound(new { Message = "Cliente não encontrado" });

                return Ok(clienteDto);
            }

            var oficinaId = _currentUserService.OficinaId;
            if (!oficinaId.HasValue)
                return Forbid();

            var clienteOficinaDto = await _clienteService.GetByIdWithDetailsForOficina(id, oficinaId.Value);

            if (clienteOficinaDto is null)
                return NotFound(new { Message = "Cliente não encontrado" });

            return Ok(clienteOficinaDto);
        }

        [HttpGet("nome/{nome}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario}")]
        public async Task<IActionResult> GetByNameWithDetails(string nome)
        {
            if (_currentUserService.IsInRole(SystemRoles.Admin))
            {
                var clientesDto = await _clienteService.GetByNameWithDetails(nome);
                if (!clientesDto.Any())
                    return NotFound(new { Message = "Nenhum cliente encontrado com esse nome" });

                return Ok(clientesDto);
            }

            var oficinaId = _currentUserService.OficinaId;
            if (!oficinaId.HasValue)
                return Forbid();

            var clientesOficinaDto = await _clienteService.GetByNameWithDetailsForOficina(nome, oficinaId.Value);

            if (!clientesOficinaDto.Any())
                return NotFound(new { Message = "Nenhum cliente encontrado com esse nome" });

            return Ok(clientesOficinaDto);
        }

        [HttpGet("oficinas/{oficinaId:int}")]
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Oficina},{SystemRoles.Funcionario}")]
        public async Task<IActionResult> GetByOficinaId(int oficinaId)
        {
            if (!_currentUserService.IsInRole(SystemRoles.Admin))
            {
                var currentOficinaId = _currentUserService.OficinaId;
                if (!currentOficinaId.HasValue || currentOficinaId.Value != oficinaId)
                    return Forbid();
            }

            var clientes = await _clienteService.GetByOficina(oficinaId);

            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = clientes;
            _response.Message = "Clientes da oficina listados com sucesso";

            return Ok(_response);
        }

        [HttpPost]
        [AllowAnonymous]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting(RateLimitPolicies.PublicRegistration)]
        public async Task<IActionResult> Post(ClienteRequestDTO clienteDTO)
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

                if (IsOficinaRegistration())
                {
                    var oficinaId = _currentUserService.OficinaId;
                    if (!oficinaId.HasValue)
                        return Forbid();

                    var cliente = await _clienteService.CreateForOficina(clienteDTO, oficinaId.Value);

                    _response.Code = ResponseEnum.SUCCESS;
                    _response.Data = cliente;
                    _response.Message = "Cliente cadastrado ou vinculado à oficina com sucesso";

                    return Ok(_response);
                }

                await _clienteService.Create(clienteDTO);

                _response.Code = ResponseEnum.SUCCESS;
                _response.Data = ToResponse(clienteDTO);
                _response.Message = "Cliente cadastrado com sucesso";

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
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Cliente}")]
        public async Task<IActionResult> Put([FromRoute] int id, [FromBody] ClienteRequestDTO clienteDTO)
        {
            if (_currentUserService.IsInRole(SystemRoles.Cliente) && _currentUserService.UserId != id)
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

                await _clienteService.Update(clienteDTO, id);

                _response.Code = ResponseEnum.SUCCESS;
                _response.Data = ToResponse(clienteDTO);
                _response.Message = "Cliente atualizado com sucesso";

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
        [Authorize(Roles = $"{SystemRoles.Admin},{SystemRoles.Cliente}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (_currentUserService.IsInRole(SystemRoles.Cliente) && _currentUserService.UserId != id)
                return Forbid();

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

        [HttpPost("login")]
        [AllowAnonymous]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting(RateLimitPolicies.ClienteLogin)]
        public async Task<ActionResult> Login([FromBody] Login login)
        {
            if (login is null)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Data = null;
                _response.Message = "Dados inválidos";

                return BadRequest(_response);
            }

            var professorDTO = await _clienteService.Login(login);

            if (professorDTO is null)
            {
                _response.Code = ResponseEnum.INVALID;
                _response.Data = null;
                _response.Message = "Email ou senha incorretos";

                return BadRequest(_response);
            }

            var token = _jwtTokenService.GenerateToken(new JwtTokenRequest
            {
                UserId = professorDTO.Id,
                Name = professorDTO.Nome,
                Email = professorDTO.Email,
                Role = SystemRoles.Cliente
            });
            _response.Code = ResponseEnum.SUCCESS;
            _response.Data = token;
            _response.Message = "Login realizado com sucesso";

            return Ok(_response);
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

        private bool IsOficinaRegistration()
        {
            return _currentUserService.IsInRole(SystemRoles.Oficina) ||
                   _currentUserService.IsInRole(SystemRoles.Funcionario);
        }

        private static ClienteDTO ToResponse(ClienteDTO clienteDTO)
        {
            return new ClienteDTO
            {
                Id = clienteDTO.Id,
                Nome = clienteDTO.Nome,
                Email = clienteDTO.Email,
                Cpf_Cnpj = clienteDTO.Cpf_Cnpj,
                Obs = clienteDTO.Obs,
                razao = clienteDTO.razao,
                DataNasc = clienteDTO.DataNasc,
                Numero = clienteDTO.Numero,
                Rua = clienteDTO.Rua,
                Cidade = clienteDTO.Cidade,
                Cep = clienteDTO.Cep,
                Bairro = clienteDTO.Bairro,
                Estado = clienteDTO.Estado,
                Pais = clienteDTO.Pais,
                Complemento = clienteDTO.Complemento,
                Sexo = clienteDTO.Sexo,
                TipoCliente = clienteDTO.TipoCliente,
                Situacao = clienteDTO.Situacao,
                Telefones = clienteDTO.Telefones
            };
        }

    }
}
