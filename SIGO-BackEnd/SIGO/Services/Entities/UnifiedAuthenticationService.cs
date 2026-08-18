using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Security;
using SIGO.Services.Interfaces;

namespace SIGO.Services.Entities
{
    public sealed class UnifiedAuthenticationService : IUnifiedAuthenticationService
    {
        private readonly IClienteAuthenticationService _clienteAuthenticationService;
        private readonly IFuncionarioService _funcionarioService;
        private readonly IOficinaService _oficinaService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IFuncionarioRoleResolver _funcionarioRoleResolver;

        public UnifiedAuthenticationService(
            IClienteAuthenticationService clienteAuthenticationService,
            IFuncionarioService funcionarioService,
            IOficinaService oficinaService,
            IJwtTokenService jwtTokenService,
            IFuncionarioRoleResolver funcionarioRoleResolver)
        {
            _clienteAuthenticationService = clienteAuthenticationService;
            _funcionarioService = funcionarioService;
            _oficinaService = oficinaService;
            _jwtTokenService = jwtTokenService;
            _funcionarioRoleResolver = funcionarioRoleResolver;
        }

        public async Task<UnifiedLoginResponseDTO?> AuthenticateAsync(
            UnifiedLoginRequestDTO request,
            CancellationToken cancellationToken = default)
        {
            var identifier = request.Identifier?.Trim() ?? string.Empty;
            var password = request.Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(identifier) ||
                string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            // Se possui @, tenta login por e-mail:
            // Funcionário/Admin primeiro e depois Oficina.
            if (identifier.Contains('@'))
            {
                var email = identifier.ToLowerInvariant();

                var funcionario = await _funcionarioService.Login(new Login
                {
                    Email = email,
                    Password = password
                });

                if (funcionario is not null)
                {
                    var role = _funcionarioRoleResolver.Resolve(funcionario.Role);

                    if (role == SystemRoles.Funcionario &&
                        !funcionario.IdOficina.HasValue)
                    {
                        return null;
                    }

                    var token = _jwtTokenService.GenerateToken(
                        new JwtTokenRequest
                        {
                            UserId = funcionario.Id,
                            Name = funcionario.Nome,
                            Email = funcionario.Email,
                            Role = role,
                            OficinaId = funcionario.IdOficina
                        });

                    return new UnifiedLoginResponseDTO(
                        token,
                        role
                    );
                }

                var oficina = await _oficinaService.Login(new Login
                {
                    Email = email,
                    Password = password
                });

                if (oficina is not null)
                {
                    var token = _jwtTokenService.GenerateToken(
                        new JwtTokenRequest
                        {
                            UserId = oficina.Id,
                            Name = oficina.Nome,
                            Email = oficina.Email,
                            Role = SystemRoles.Oficina,
                            OficinaId = oficina.Id
                        });

                    return new UnifiedLoginResponseDTO(
                        token,
                        SystemRoles.Oficina
                    );
                }

                return null;
            }

            // Se não possui @, considera CPF/CNPJ.
            var documento = new string(
                identifier.Where(char.IsDigit).ToArray()
            );

            if (documento.Length != 11 && documento.Length != 14)
            {
                return null;
            }

            var cliente = await _clienteAuthenticationService.AuthenticateAsync(
                new LoginClienteDTO
                {
                    Cpf_Cnpj = documento,
                    Senha = password
                },
                cancellationToken
            );

            if (cliente is null)
            {
                return null;
            }

            var clienteToken = _jwtTokenService.GenerateToken(
                new JwtTokenRequest
                {
                    UserId = cliente.ClienteId,
                    Name = cliente.Nome,
                    Email = cliente.Email,
                    Role = SystemRoles.Cliente,
                    TokenVersion = cliente.TokenVersion
                });

            return new UnifiedLoginResponseDTO(
                clienteToken,
                SystemRoles.Cliente
            );
        }
    }
}