using SIGO.Data.Interfaces;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Enums;
using SIGO.Objects.Models;
using SIGO.Security;
using SIGO.Services.Interfaces;
using SIGO.Validation;
using SIGO.Exceptions;

namespace SIGO.Services.Entities
{
    public sealed class ClienteAuthenticationService : IClienteAuthenticationService
    {
        private static readonly Lazy<string> DummyPasswordHash = new(
            () => BCrypt.Net.BCrypt.HashPassword("sigo-login-timing-placeholder", 12),
            LazyThreadSafetyMode.ExecutionAndPublication);

        private readonly IClienteContaRepository _contaRepository;
        private readonly IClienteIdentityRepository _identityRepository;
        private readonly ICpfValidator _cpfValidator;
        private readonly IPasswordHasher _passwordHasher;
        private readonly TimeProvider _timeProvider;

        public ClienteAuthenticationService(
            IClienteContaRepository contaRepository,
            IClienteIdentityRepository identityRepository,
            ICpfValidator cpfValidator,
            IPasswordHasher passwordHasher,
            TimeProvider timeProvider)
        {
            _contaRepository = contaRepository;
            _identityRepository = identityRepository;
            _cpfValidator = cpfValidator;
            _passwordHasher = passwordHasher;
            _timeProvider = timeProvider;
        }

        public async Task<ClienteAuthenticationResult?> AuthenticateAsync(
            LoginClienteDTO login,
            CancellationToken cancellationToken = default)
        {
            if (login is null ||
                !_cpfValidator.IsValid(login.Cpf) ||
                string.IsNullOrWhiteSpace(login.Senha) ||
                login.Senha.Length > 128)
            {
                _passwordHasher.Verify(login?.Senha ?? string.Empty, DummyPasswordHash.Value);
                return null;
            }

            var cpf = _cpfValidator.Normalize(login.Cpf);
            var cliente = await _identityRepository.GetClienteByCpfAsync(cpf, cancellationToken);
            var conta = cliente?.Conta;
            if (conta is null ||
                conta.Status != EstadoClienteConta.Active ||
                cliente!.Situacao != Situacao.ATIVO)
            {
                _passwordHasher.Verify(login.Senha, DummyPasswordHash.Value);
                return null;
            }

            if (!_passwordHasher.Verify(login.Senha, conta.PasswordHash))
                return null;

            if (_passwordHasher.NeedsRehash(conta.PasswordHash))
            {
                var rehashedPassword = _passwordHasher.Hash(login.Senha);
                var updated = await _contaRepository.TryUpdatePasswordAsync(
                    conta.ClienteId,
                    conta.PasswordHash,
                    conta.TokenVersion,
                    rehashedPassword,
                    UtcNow(),
                    cancellationToken);

                if (updated)
                {
                    conta.PasswordHash = rehashedPassword;
                    conta.TokenVersion++;
                }
                else
                {
                    cliente = await _identityRepository.GetClienteByCpfAsync(cpf, cancellationToken);
                    conta = cliente?.Conta;
                    if (conta is null ||
                        conta.Status != EstadoClienteConta.Active ||
                        cliente!.Situacao != Situacao.ATIVO ||
                        !_passwordHasher.Verify(login.Senha, conta.PasswordHash))
                    {
                        return null;
                    }
                }
            }

            return new ClienteAuthenticationResult(
                conta.ClienteId,
                cliente!.Nome,
                conta.EmailNormalizado,
                conta.TokenVersion);
        }

        public async Task ChangePasswordAsync(
            int clienteId,
            AlterarSenhaClienteDTO request,
            SecurityAuditContext auditContext,
            CancellationToken cancellationToken = default)
        {
            var conta = await _contaRepository.GetByClienteIdAsync(clienteId, cancellationToken);
            var valid = conta is not null &&
                        conta.Status == EstadoClienteConta.Active &&
                        _passwordHasher.Verify(request?.SenhaAtual, conta.PasswordHash);

            if (!valid)
            {
                await AddPasswordAuditAsync(
                    clienteId,
                    auditContext,
                    ResultadoAuditoria.Falha,
                    cancellationToken);
                throw new BusinessValidationException(new[]
                {
                    new ValidationError(nameof(AlterarSenhaClienteDTO.SenhaAtual), "Senha atual inválida.")
                });
            }

            EnsureNewPasswordIsValid(request);

            var updated = await _identityRepository.ExecuteInTransactionAsync(
                async token =>
                {
                    var passwordUpdated = await _contaRepository.TryUpdatePasswordAsync(
                        clienteId,
                        conta!.PasswordHash,
                        conta.TokenVersion,
                        _passwordHasher.Hash(request.NovaSenha),
                        UtcNow(),
                        token);
                    if (!passwordUpdated)
                        return false;

                    await AddPasswordAuditAsync(
                        clienteId,
                        auditContext,
                        ResultadoAuditoria.Sucesso,
                        token);
                    return true;
                },
                cancellationToken);

            if (!updated)
                throw new ConflictException("A senha foi alterada por outra sessão. Tente novamente.");
        }

        private async Task AddPasswordAuditAsync(
            int clienteId,
            SecurityAuditContext auditContext,
            ResultadoAuditoria result,
            CancellationToken cancellationToken)
        {
            await _identityRepository.AddAuditoriaAsync(new AuditoriaSeguranca
            {
                ClienteId = clienteId,
                TipoAtor = auditContext.TipoAtor,
                AtorId = auditContext.AtorId,
                Evento = TipoEventoAuditoria.SenhaAlterada,
                Resultado = result,
                IpAddress = auditContext.IpAddress,
                CorrelationId = auditContext.CorrelationId,
                CreatedAt = UtcNow()
            }, cancellationToken);
            await _identityRepository.SaveChangesAsync(cancellationToken);
        }

        private static void EnsureNewPasswordIsValid(AlterarSenhaClienteDTO request)
        {
            var password = request?.NovaSenha;
            if (!string.IsNullOrWhiteSpace(password) &&
                password.Length is >= 8 and <= 128 &&
                password.Any(char.IsLetter) &&
                password.Any(char.IsDigit) &&
                !string.Equals(password, request.SenhaAtual, StringComparison.Ordinal))
            {
                return;
            }

            throw new BusinessValidationException(new[]
            {
                new ValidationError(
                    nameof(AlterarSenhaClienteDTO.NovaSenha),
                    "Nova senha deve ter entre 8 e 128 caracteres, uma letra, um número e ser diferente da atual.")
            });
        }

        private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;
    }
}
