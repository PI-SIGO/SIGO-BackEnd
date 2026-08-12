using Moq;
using SIGO.Data.Interfaces;
using SIGO.Exceptions;
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
    public sealed class ClienteRegistrationServiceTests
    {
        private static readonly DateTime RegistrationTime =
            new(2026, 7, 13, 12, 30, 0, DateTimeKind.Utc);

        private readonly Mock<IClienteIdentityRepository> _identityRepositoryMock = new();
        private readonly Mock<IClienteContaRepository> _contaRepositoryMock = new();
        private readonly Mock<IPasswordHasher> _passwordHasherMock = new();

        public ClienteRegistrationServiceTests()
        {
            _identityRepositoryMock
                .Setup(repository => repository.ExecuteInTransactionAsync(
                    It.IsAny<Func<CancellationToken, Task<CadastroClienteResultadoDTO>>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<Func<CancellationToken, Task<CadastroClienteResultadoDTO>>, CancellationToken>(
                    (operation, token) => operation(token));
            _identityRepositoryMock
                .Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            _contaRepositoryMock
                .Setup(repository => repository.EmailInUseByOtherClienteAsync(
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _passwordHasherMock
                .Setup(hasher => hasher.Hash("Senha123"))
                .Returns("hash-seguro");
        }

        [Fact]
        public async Task RegisterAsync_CpfPreCadastradoSemContaDeveCriarContaNoMesmoCliente()
        {
            var veiculos = new List<Veiculo>
            {
                new() { Id = 10, ClienteId = 42, PlacaVeiculo = "ABC1D23" },
                new() { Id = 11, ClienteId = 42, PlacaVeiculo = "XYZ9A87" }
            };
            var cliente = CreateCliente(42, Situacao.ATIVO);
            cliente.Senha = "hash-legado-da-oficina";
            cliente.Veiculos = veiculos;
            ClienteConta? contaCriada = null;
            _identityRepositoryMock
                .Setup(repository => repository.GetClienteByCpfAsync(
                    "52998224725",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(cliente);
            _contaRepositoryMock
                .Setup(repository => repository.GetByClienteIdAsync(
                    42,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((ClienteConta?)null);
            _contaRepositoryMock
                .Setup(repository => repository.AddAsync(
                    It.IsAny<ClienteConta>(),
                    It.IsAny<CancellationToken>()))
                .Callback<ClienteConta, CancellationToken>((conta, _) => contaCriada = conta)
                .Returns(Task.CompletedTask);

            var result = await CreateService().RegisterAsync(
                CreateRequest(),
                CreateAuditContext());

            Assert.Equal(42, result.ClienteId);
            Assert.Same(veiculos, cliente.Veiculos);
            Assert.All(cliente.Veiculos, veiculo => Assert.Equal(42, veiculo.ClienteId));
            Assert.Equal("cliente@example.com", cliente.Email);
            Assert.Null(cliente.Senha);
            Assert.NotNull(contaCriada);
            Assert.Equal(42, contaCriada!.ClienteId);
            Assert.Equal("cliente@example.com", contaCriada.EmailNormalizado);
            Assert.Equal("hash-seguro", contaCriada.PasswordHash);
            Assert.Equal(EstadoClienteConta.Active, contaCriada.Status);
            _identityRepositoryMock.Verify(repository => repository.AddClienteAsync(
                It.IsAny<Cliente>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_CpfNovoDeveCriarClienteContaAtivaEContatoNaoVerificado()
        {
            Cliente? clienteCriado = null;
            ClienteConta? contaCriada = null;
            ClienteContato? contatoCriado = null;
            AuditoriaSeguranca? auditoriaCriada = null;
            _identityRepositoryMock
                .Setup(repository => repository.GetClienteByCpfAsync(
                    "52998224725",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Cliente?)null);
            _identityRepositoryMock
                .Setup(repository => repository.AddClienteAsync(
                    It.IsAny<Cliente>(),
                    It.IsAny<CancellationToken>()))
                .Callback<Cliente, CancellationToken>((cliente, _) =>
                {
                    cliente.Id = 84;
                    clienteCriado = cliente;
                })
                .Returns(Task.CompletedTask);
            _contaRepositoryMock
                .Setup(repository => repository.AddAsync(
                    It.IsAny<ClienteConta>(),
                    It.IsAny<CancellationToken>()))
                .Callback<ClienteConta, CancellationToken>((conta, _) => contaCriada = conta)
                .Returns(Task.CompletedTask);
            _identityRepositoryMock
                .Setup(repository => repository.GetContatoAsync(
                    84,
                    TipoContatoCliente.Email,
                    "cliente@example.com",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((ClienteContato?)null);
            _identityRepositoryMock
                .Setup(repository => repository.AddContatoAsync(
                    It.IsAny<ClienteContato>(),
                    It.IsAny<CancellationToken>()))
                .Callback<ClienteContato, CancellationToken>((contato, _) => contatoCriado = contato)
                .Returns(Task.CompletedTask);
            _identityRepositoryMock
                .Setup(repository => repository.AddAuditoriaAsync(
                    It.IsAny<AuditoriaSeguranca>(),
                    It.IsAny<CancellationToken>()))
                .Callback<AuditoriaSeguranca, CancellationToken>((auditoria, _) => auditoriaCriada = auditoria)
                .Returns(Task.CompletedTask);

            var result = await CreateService().RegisterAsync(
                CreateRequest(),
                CreateAuditContext());

            Assert.Equal(new CadastroClienteResultadoDTO(84, "Cliente Teste", "cliente@example.com"), result);
            Assert.NotNull(clienteCriado);
            Assert.Equal("52998224725", clienteCriado!.Cpf_Cnpj);
            Assert.Equal("cliente@example.com", clienteCriado.Email);
            Assert.Null(clienteCriado.Senha);

            Assert.NotNull(contaCriada);
            Assert.Equal(84, contaCriada!.ClienteId);
            Assert.Equal("cliente@example.com", contaCriada.EmailNormalizado);
            Assert.Equal("hash-seguro", contaCriada.PasswordHash);
            Assert.Equal(EstadoClienteConta.Active, contaCriada.Status);
            Assert.Equal(RegistrationTime, contaCriada.CreatedAt);
            Assert.Equal(RegistrationTime, contaCriada.UpdatedAt);

            Assert.NotNull(contatoCriado);
            Assert.Equal(84, contatoCriado!.ClienteId);
            Assert.Equal(OrigemContatoCliente.Cliente, contatoCriado.Origem);
            Assert.Null(contatoCriado.VerificadoEm);
            Assert.Equal(RegistrationTime, contatoCriado.CreatedAt);

            Assert.NotNull(auditoriaCriada);
            Assert.Equal(TipoEventoAuditoria.CadastroDiretoConcluido, auditoriaCriada!.Evento);
            Assert.Equal("***.***.725-**", auditoriaCriada.DocumentoMascarado);
            Assert.Equal("c***@example.com", auditoriaCriada.ContatoMascarado);
            Assert.Null(auditoriaCriada.DocumentoHash);
            Assert.Null(auditoriaCriada.ContatoHash);
            _identityRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task RegisterAsync_DeveRejeitarCpfQueJaPossuiConta()
        {
            var cliente = CreateCliente(42, Situacao.ATIVO);
            _identityRepositoryMock
                .Setup(repository => repository.GetClienteByCpfAsync(
                    "52998224725",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(cliente);
            _contaRepositoryMock
                .Setup(repository => repository.GetByClienteIdAsync(42, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ClienteConta
                {
                    ClienteId = 42,
                    EmailNormalizado = "existente@example.com",
                    PasswordHash = "hash-existente",
                    Status = EstadoClienteConta.Active
                });

            await Assert.ThrowsAsync<ConflictException>(() => CreateService().RegisterAsync(
                CreateRequest(),
                CreateAuditContext()));

            VerifyNoIdentityWasCreated();
        }

        [Fact]
        public async Task RegisterAsync_DeveRejeitarEmailUsadoPorOutroCliente()
        {
            _identityRepositoryMock
                .Setup(repository => repository.GetClienteByCpfAsync(
                    "52998224725",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Cliente?)null);
            _contaRepositoryMock
                .Setup(repository => repository.EmailInUseByOtherClienteAsync(
                    "cliente@example.com",
                    null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            await Assert.ThrowsAsync<ConflictException>(() => CreateService().RegisterAsync(
                CreateRequest(),
                CreateAuditContext()));

            VerifyNoIdentityWasCreated();
        }

        [Fact]
        public async Task RegisterAsync_DeveRejeitarClienteInativo()
        {
            _identityRepositoryMock
                .Setup(repository => repository.GetClienteByCpfAsync(
                    "52998224725",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateCliente(42, Situacao.INATIVO));

            await Assert.ThrowsAsync<ConflictException>(() => CreateService().RegisterAsync(
                CreateRequest(),
                CreateAuditContext()));

            VerifyNoIdentityWasCreated();
            _contaRepositoryMock.Verify(repository => repository.EmailInUseByOtherClienteAsync(
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private ClienteRegistrationService CreateService()
        {
            return new ClienteRegistrationService(
                _identityRepositoryMock.Object,
                _contaRepositoryMock.Object,
                new CpfValidator(),
                _passwordHasherMock.Object,
                new FixedTimeProvider(RegistrationTime));
        }

        private static CadastrarClienteDTO CreateRequest()
        {
            return new CadastrarClienteDTO
            {
                Cpf = "529.982.247-25",
                Nome = "  Cliente Teste  ",
                Email = "  Cliente@Example.com  ",
                Senha = "Senha123"
            };
        }

        private static Cliente CreateCliente(int id, Situacao situacao)
        {
            return new Cliente
            {
                Id = id,
                Nome = "Cliente Teste",
                Email = "original@example.com",
                Cpf_Cnpj = "52998224725",
                Senha = null,
                Situacao = situacao
            };
        }

        private static SecurityAuditContext CreateAuditContext()
        {
            return new SecurityAuditContext(
                TipoAtorAuditoria.Anonimo,
                null,
                "127.0.0.1",
                "cadastro-request");
        }

        private void VerifyNoIdentityWasCreated()
        {
            _identityRepositoryMock.Verify(repository => repository.AddClienteAsync(
                It.IsAny<Cliente>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
            _contaRepositoryMock.Verify(repository => repository.AddAsync(
                It.IsAny<ClienteConta>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
            _identityRepositoryMock.Verify(repository => repository.AddContatoAsync(
                It.IsAny<ClienteContato>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private sealed class FixedTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _utcNow;

            public FixedTimeProvider(DateTime utcNow)
            {
                _utcNow = new DateTimeOffset(utcNow);
            }

            public override DateTimeOffset GetUtcNow() => _utcNow;
        }
    }
}
