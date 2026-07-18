using Moq;
using SIGO.Data.Interfaces;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Enums;
using SIGO.Objects.Models;
using SIGO.Security;
using SIGO.Services.Entities;
using SIGO.Validation;
using Xunit;

namespace SIGO.Tests.Services
{
    public class ClienteAuthenticationServiceTests
    {
        private readonly Mock<IClienteContaRepository> _contaRepositoryMock = new();
        private readonly Mock<IClienteIdentityRepository> _identityRepositoryMock = new();
        private readonly Mock<IPasswordHasher> _passwordHasherMock = new();

        [Fact]
        public async Task AuthenticateAsync_DeveNormalizarCpfERetornarContaAtiva()
        {
            var cliente = CreateActiveClient();
            _identityRepositoryMock
                .Setup(repository => repository.GetClienteByCpfAsync(
                    "52998224725",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(cliente);
            _passwordHasherMock.Setup(hasher => hasher.Verify("Senha123", "hash-atual")).Returns(true);
            _passwordHasherMock.Setup(hasher => hasher.NeedsRehash("hash-atual")).Returns(false);

            var result = await CreateService().AuthenticateAsync(new LoginClienteDTO
            {
                Cpf = "529.982.247-25",
                Senha = "Senha123"
            });

            Assert.NotNull(result);
            Assert.Equal(42, result!.ClienteId);
            Assert.Equal("Cliente Existente", result.Nome);
            Assert.Equal("cliente@example.com", result.Email);
            Assert.Equal(3, result.TokenVersion);
            _contaRepositoryMock.Verify(
                repository => repository.GetByEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task AuthenticateAsync_DeveRejeitarContaBloqueada()
        {
            var cliente = CreateActiveClient();
            cliente.Conta.Status = EstadoClienteConta.Blocked;
            _identityRepositoryMock
                .Setup(repository => repository.GetClienteByCpfAsync(
                    "52998224725",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(cliente);

            var result = await CreateService().AuthenticateAsync(new LoginClienteDTO
            {
                Cpf = "52998224725",
                Senha = "Senha123"
            });

            Assert.Null(result);
            _passwordHasherMock.Verify(
                hasher => hasher.Verify(It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task AuthenticateAsync_DeveRejeitarSenhaInvalida()
        {
            var cliente = CreateActiveClient();
            _identityRepositoryMock
                .Setup(repository => repository.GetClienteByCpfAsync(
                    "52998224725",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(cliente);
            _passwordHasherMock.Setup(hasher => hasher.Verify("senha-errada", "hash-atual")).Returns(false);

            var result = await CreateService().AuthenticateAsync(new LoginClienteDTO
            {
                Cpf = "52998224725",
                Senha = "senha-errada"
            });

            Assert.Null(result);
        }

        [Fact]
        public async Task AuthenticateAsync_DeveAtualizarHashETokenVersion_QuandoHashEstaDefasado()
        {
            var cliente = CreateActiveClient();
            var conta = cliente.Conta;
            _identityRepositoryMock
                .Setup(repository => repository.GetClienteByCpfAsync(
                    "52998224725",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(cliente);
            _passwordHasherMock.Setup(hasher => hasher.Verify("Senha123", "hash-atual")).Returns(true);
            _passwordHasherMock.Setup(hasher => hasher.NeedsRehash("hash-atual")).Returns(true);
            _passwordHasherMock.Setup(hasher => hasher.Hash("Senha123")).Returns("hash-novo");
            _contaRepositoryMock
                .Setup(repository => repository.TryUpdatePasswordAsync(
                    42,
                    "hash-atual",
                    3,
                    "hash-novo",
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await CreateService().AuthenticateAsync(new LoginClienteDTO
            {
                Cpf = "52998224725",
                Senha = "Senha123"
            });

            Assert.NotNull(result);
            Assert.Equal("hash-novo", conta.PasswordHash);
            Assert.Equal(4, conta.TokenVersion);
            Assert.Equal(4, result!.TokenVersion);
            _contaRepositoryMock.Verify(
                repository => repository.TryUpdatePasswordAsync(
                    42,
                    "hash-atual",
                    3,
                    "hash-novo",
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_DeveRejeitarSenhaAtualInvalidaEAuditarFalha()
        {
            var conta = CreateActiveAccount();
            AuditoriaSeguranca capturedAudit = null;
            _contaRepositoryMock
                .Setup(repository => repository.GetByClienteIdAsync(42, It.IsAny<CancellationToken>()))
                .ReturnsAsync(conta);
            _passwordHasherMock.Setup(hasher => hasher.Verify("Errada123", "hash-atual")).Returns(false);
            _identityRepositoryMock
                .Setup(repository => repository.AddAuditoriaAsync(
                    It.IsAny<AuditoriaSeguranca>(),
                    It.IsAny<CancellationToken>()))
                .Callback<AuditoriaSeguranca, CancellationToken>((audit, _) => capturedAudit = audit)
                .Returns(Task.CompletedTask);

            var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
                CreateService().ChangePasswordAsync(
                    42,
                    new AlterarSenhaClienteDTO { SenhaAtual = "Errada123", NovaSenha = "NovaSenha123" },
                    CreateAuditContext()));

            Assert.Contains(exception.Errors, error => error.Field == nameof(AlterarSenhaClienteDTO.SenhaAtual));
            Assert.NotNull(capturedAudit);
            Assert.Equal(ResultadoAuditoria.Falha, capturedAudit!.Resultado);
            Assert.Equal(TipoEventoAuditoria.SenhaAlterada, capturedAudit.Evento);
            Assert.Equal("hash-atual", conta.PasswordHash);
        }

        [Fact]
        public async Task ChangePasswordAsync_DeveTrocarHashIncrementarVersaoEAuditarSucesso()
        {
            var conta = CreateActiveAccount();
            AuditoriaSeguranca capturedAudit = null;
            _contaRepositoryMock
                .Setup(repository => repository.GetByClienteIdAsync(42, It.IsAny<CancellationToken>()))
                .ReturnsAsync(conta);
            _passwordHasherMock.Setup(hasher => hasher.Verify("Senha123", "hash-atual")).Returns(true);
            _passwordHasherMock.Setup(hasher => hasher.Hash("NovaSenha456")).Returns("hash-novo");
            _contaRepositoryMock
                .Setup(repository => repository.TryUpdatePasswordAsync(
                    42,
                    "hash-atual",
                    3,
                    "hash-novo",
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .Callback(() =>
                {
                    conta.PasswordHash = "hash-novo";
                    conta.TokenVersion = 4;
                })
                .ReturnsAsync(true);
            _identityRepositoryMock
                .Setup(repository => repository.ExecuteInTransactionAsync(
                    It.IsAny<Func<CancellationToken, Task<bool>>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<Func<CancellationToken, Task<bool>>, CancellationToken>((operation, token) => operation(token));
            _identityRepositoryMock
                .Setup(repository => repository.AddAuditoriaAsync(
                    It.IsAny<AuditoriaSeguranca>(),
                    It.IsAny<CancellationToken>()))
                .Callback<AuditoriaSeguranca, CancellationToken>((audit, _) => capturedAudit = audit)
                .Returns(Task.CompletedTask);

            await CreateService().ChangePasswordAsync(
                42,
                new AlterarSenhaClienteDTO { SenhaAtual = "Senha123", NovaSenha = "NovaSenha456" },
                CreateAuditContext());

            Assert.Equal("hash-novo", conta.PasswordHash);
            Assert.Equal(4, conta.TokenVersion);
            Assert.NotNull(capturedAudit);
            Assert.Equal(ResultadoAuditoria.Sucesso, capturedAudit!.Resultado);
            _identityRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private ClienteAuthenticationService CreateService()
        {
            return new ClienteAuthenticationService(
                _contaRepositoryMock.Object,
                _identityRepositoryMock.Object,
                new CpfValidator(),
                _passwordHasherMock.Object,
                TimeProvider.System);
        }

        private static ClienteConta CreateActiveAccount()
        {
            return CreateActiveClient().Conta;
        }

        private static Cliente CreateActiveClient()
        {
            var cliente = new Cliente
            {
                Id = 42,
                Nome = "Cliente Existente",
                Situacao = Situacao.ATIVO
            };
            cliente.Conta = new ClienteConta
            {
                Id = 1,
                ClienteId = cliente.Id,
                Cliente = cliente,
                EmailNormalizado = "cliente@example.com",
                PasswordHash = "hash-atual",
                Status = EstadoClienteConta.Active,
                TokenVersion = 3
            };
            return cliente;
        }

        private static SecurityAuditContext CreateAuditContext()
        {
            return new SecurityAuditContext(
                TipoAtorAuditoria.Cliente,
                42,
                "127.0.0.1",
                "correlation-id");
        }
    }
}
