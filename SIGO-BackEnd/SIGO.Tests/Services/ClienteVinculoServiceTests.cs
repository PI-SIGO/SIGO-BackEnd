using Moq;
using SIGO.Data.Interfaces;
using SIGO.Exceptions;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Enums;
using SIGO.Objects.Models;
using SIGO.Services.Entities;
using SIGO.Validation;
using Xunit;

namespace SIGO.Tests.Services;

public sealed class ClienteVinculoServiceTests
{
    [Fact]
    public async Task PreRegisterAsync_CpfNovo_DeveCriarClienteComPerfilCompletoETelefone()
    {
        var request = CreateFullPreRegistration();
        var identityRepository = CreateTransactionalIdentityRepository();
        var vinculoRepository = new Mock<IClienteOficinaRepository>();
        Cliente? createdCliente = null;
        var contacts = new List<ClienteContato>();

        identityRepository
            .Setup(repository => repository.GetClienteByCpfCnpjAsync(
                "52998224725",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cliente?)null);
        identityRepository
            .Setup(repository => repository.AddClienteAsync(
                It.IsAny<Cliente>(),
                It.IsAny<CancellationToken>()))
            .Callback<Cliente, CancellationToken>((cliente, _) =>
            {
                cliente.Id = 42;
                createdCliente = cliente;
            })
            .Returns(Task.CompletedTask);
        identityRepository
            .Setup(repository => repository.AddContatoAsync(
                It.IsAny<ClienteContato>(),
                It.IsAny<CancellationToken>()))
            .Callback<ClienteContato, CancellationToken>((contact, _) => contacts.Add(contact))
            .Returns(Task.CompletedTask);
        vinculoRepository
            .Setup(repository => repository.AddOrActivateAsync(7, 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClienteOficina { OficinaId = 7, ClienteId = 42, Ativo = true });
        var service = CreateService(identityRepository, vinculoRepository);

        var result = await service.PreRegisterAsync(
            request,
            7,
            new SecurityAuditContext(TipoAtorAuditoria.Oficina, 7, "127.0.0.1", "test"));

        Assert.Equal(42, result.ClienteId);
        Assert.NotNull(createdCliente);
        Assert.Equal("Cliente Completo", createdCliente!.Nome);
        Assert.Equal("52998224725", createdCliente.Cpf_Cnpj);
        Assert.Equal("cliente@example.com", createdCliente.Email);
        Assert.Null(createdCliente.Senha);
        Assert.Equal("Observação da oficina", createdCliente.Obs);
        Assert.Equal(new DateOnly(1950, 5, 10), createdCliente.DataNasc);
        Assert.Equal(Sexo.Feminino, createdCliente.Sexo);
        Assert.Equal(TipoCliente.FISICO, createdCliente.TipoCliente);
        Assert.Equal("89010000", createdCliente.Cep);
        Assert.Equal("Rua das Flores", createdCliente.Rua);
        Assert.Equal(120, createdCliente.Numero);
        var phone = Assert.Single(createdCliente.Telefones);
        Assert.Equal(47, phone.DDD);
        Assert.Equal("999999999", phone.Numero);
        Assert.Contains(contacts, contact =>
            contact.Tipo == TipoContatoCliente.Email &&
            contact.ValorNormalizado == "cliente@example.com" &&
            contact.Origem == OrigemContatoCliente.Oficina &&
            contact.VerificadoEm is null);
        Assert.Contains(contacts, contact =>
            contact.Tipo == TipoContatoCliente.Telefone &&
            contact.ValorNormalizado == "47999999999" &&
            contact.Origem == OrigemContatoCliente.Oficina &&
            contact.VerificadoEm is null);
    }

    [Fact]
    public async Task PreRegisterAsync_CnpjNovo_DeveCriarClienteJuridico()
    {
        var request = CreateFullPreRegistration() with
        {
            Cpf = null,
            Cpf_Cnpj = "11.222.333/0001-81",
            Nome = "Empresa Completa"
        };
        var identityRepository = CreateTransactionalIdentityRepository();
        var vinculoRepository = new Mock<IClienteOficinaRepository>();
        Cliente? createdCliente = null;

        identityRepository
            .Setup(repository => repository.GetClienteByCpfCnpjAsync(
                "11222333000181",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cliente?)null);
        identityRepository
            .Setup(repository => repository.AddClienteAsync(
                It.IsAny<Cliente>(),
                It.IsAny<CancellationToken>()))
            .Callback<Cliente, CancellationToken>((cliente, _) =>
            {
                cliente.Id = 43;
                createdCliente = cliente;
            })
            .Returns(Task.CompletedTask);
        vinculoRepository
            .Setup(repository => repository.AddOrActivateAsync(7, 43, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClienteOficina { OficinaId = 7, ClienteId = 43, Ativo = true });
        var service = CreateService(identityRepository, vinculoRepository);

        var result = await service.PreRegisterAsync(
            request,
            7,
            new SecurityAuditContext(TipoAtorAuditoria.Oficina, 7, "127.0.0.1", "test"));

        Assert.Equal(43, result.ClienteId);
        Assert.Equal("11222333000181", result.Cpf_Cnpj);
        Assert.NotNull(createdCliente);
        Assert.Equal("11222333000181", createdCliente!.Cpf_Cnpj);
        Assert.Equal(TipoCliente.JURIDICO, createdCliente.TipoCliente);
    }

    [Fact]
    public async Task PreRegisterAsync_DeveReutilizarClienteELigarOficinaDiretamente()
    {
        var request = new PreCadastrarClienteDTO
        {
            Cpf = "52998224725",
            Nome = "Nome informado pela oficina"
        };
        var clienteExistente = new Cliente
        {
            Id = 42,
            Nome = "Nome já cadastrado",
            Cpf_Cnpj = request.Documento,
            Situacao = Situacao.ATIVO
        };
        var identityRepository = CreateTransactionalIdentityRepository();
        var vinculoRepository = new Mock<IClienteOficinaRepository>();
        identityRepository
            .Setup(repository => repository.GetClienteByCpfCnpjAsync(request.Documento, It.IsAny<CancellationToken>()))
            .ReturnsAsync(clienteExistente);
        vinculoRepository
            .Setup(repository => repository.AddOrActivateAsync(7, 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClienteOficina
            {
                OficinaId = 7,
                ClienteId = 42,
                Ativo = true
            });
        var service = CreateService(identityRepository, vinculoRepository);

        var result = await service.PreRegisterAsync(
            request,
            7,
            new SecurityAuditContext(TipoAtorAuditoria.Oficina, 7, "127.0.0.1", "test"));

        Assert.Equal(42, result.ClienteId);
        Assert.Equal(clienteExistente.Nome, result.Nome);
        Assert.True(result.VinculoAtivo);
        vinculoRepository.Verify(repository => repository.AddOrActivateAsync(
            7,
            42,
            It.IsAny<CancellationToken>()), Times.Once);
        identityRepository.Verify(repository => repository.AddClienteAsync(
            It.IsAny<Cliente>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PreRegisterAsync_DeveImpedirReativacaoQuandoClienteRevogouVinculo()
    {
        var request = new PreCadastrarClienteDTO
        {
            Cpf = "52998224725",
            Nome = "Cliente"
        };
        var cliente = new Cliente
        {
            Id = 42,
            Nome = "Cliente",
            Cpf_Cnpj = request.Documento,
            Situacao = Situacao.ATIVO
        };
        var identityRepository = CreateTransactionalIdentityRepository();
        var vinculoRepository = new Mock<IClienteOficinaRepository>();
        identityRepository
            .Setup(repository => repository.GetClienteByCpfCnpjAsync(request.Documento, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliente);
        vinculoRepository
            .Setup(repository => repository.GetAsync(7, 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClienteOficina
            {
                OficinaId = 7,
                ClienteId = 42,
                Ativo = false,
                RevogadoEm = DateTime.UtcNow
            });
        var service = CreateService(identityRepository, vinculoRepository);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => service.PreRegisterAsync(
            request,
            7,
            new SecurityAuditContext(TipoAtorAuditoria.Oficina, 7, "127.0.0.1", "test")));

        Assert.Contains("revogou", exception.Message, StringComparison.OrdinalIgnoreCase);
        vinculoRepository.Verify(repository => repository.AddOrActivateAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PreRegisterAsync_ClienteSemConta_DeveCompletarSomenteCamposVazios()
    {
        var request = CreateFullPreRegistration();
        var cliente = new Cliente
        {
            Id = 42,
            Nome = "Nome preservado",
            Cpf_Cnpj = request.Documento,
            Email = null!,
            Obs = "Observação preservada",
            Sexo = Sexo.Outro,
            TipoCliente = TipoCliente.FISICO,
            Situacao = Situacao.ATIVO,
            Telefones = new List<Telefone>()
        };
        var identityRepository = CreateTransactionalIdentityRepository();
        var vinculoRepository = CreateLinkRepository(cliente, identityRepository);
        var service = CreateService(identityRepository, vinculoRepository);

        await service.PreRegisterAsync(
            request,
            7,
            new SecurityAuditContext(TipoAtorAuditoria.Oficina, 7, "127.0.0.1", "test"));

        Assert.Equal("Nome preservado", cliente.Nome);
        Assert.Equal("Observação preservada", cliente.Obs);
        Assert.Equal("cliente@example.com", cliente.Email);
        Assert.Equal(request.DataNasc, cliente.DataNasc);
        Assert.Equal(Sexo.Feminino, cliente.Sexo);
        Assert.Equal("Rua das Flores", cliente.Rua);
        Assert.Equal("89010000", cliente.Cep);
        Assert.Single(cliente.Telefones);
    }

    [Fact]
    public async Task PreRegisterAsync_ClienteComConta_DevePreservarPerfilETelefones()
    {
        var request = CreateFullPreRegistration() with
        {
            Nome = "Nome divergente",
            Email = "oficina@example.com",
            Rua = "Outra rua",
            Telefones = new[]
            {
                new PreCadastrarTelefoneClienteDTO { DDD = 11, Numero = "98888-7777" }
            }
        };
        var existingPhone = new Telefone { Id = 9, DDD = 47, Numero = "33334444", ClienteId = 42 };
        var cliente = new Cliente
        {
            Id = 42,
            Nome = "Nome do titular",
            Cpf_Cnpj = request.Documento,
            Email = "titular@example.com",
            Rua = "Rua do titular",
            Numero = 10,
            Sexo = Sexo.Masculino,
            TipoCliente = TipoCliente.FISICO,
            Situacao = Situacao.ATIVO,
            Conta = new ClienteConta
            {
                ClienteId = 42,
                EmailNormalizado = "titular@example.com",
                PasswordHash = "hash-preservado",
                Status = EstadoClienteConta.Active,
                TokenVersion = 3
            },
            Telefones = new List<Telefone> { existingPhone }
        };
        var identityRepository = CreateTransactionalIdentityRepository();
        var vinculoRepository = CreateLinkRepository(cliente, identityRepository);
        var service = CreateService(identityRepository, vinculoRepository);

        await service.PreRegisterAsync(
            request,
            7,
            new SecurityAuditContext(TipoAtorAuditoria.Oficina, 7, "127.0.0.1", "test"));

        Assert.Equal("Nome do titular", cliente.Nome);
        Assert.Equal("titular@example.com", cliente.Email);
        Assert.Equal("Rua do titular", cliente.Rua);
        Assert.Equal(10, cliente.Numero);
        Assert.Equal(Sexo.Masculino, cliente.Sexo);
        Assert.Same(existingPhone, Assert.Single(cliente.Telefones));
        Assert.Equal("hash-preservado", cliente.Conta.PasswordHash);
        Assert.Equal(3, cliente.Conta.TokenVersion);
        identityRepository.Verify(repository => repository.AddClienteAsync(
            It.IsAny<Cliente>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeactivateForOficinaAsync_DeveDesativarSomenteVinculoDaOficina()
    {
        var identityRepository = CreateBooleanTransactionalIdentityRepository();
        var vinculoRepository = new Mock<IClienteOficinaRepository>();
        AuditoriaSeguranca? audit = null;
        vinculoRepository
            .Setup(repository => repository.DeactivateByOficinaAsync(
                7,
                42,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        identityRepository
            .Setup(repository => repository.AddAuditoriaAsync(
                It.IsAny<AuditoriaSeguranca>(),
                It.IsAny<CancellationToken>()))
            .Callback<AuditoriaSeguranca, CancellationToken>((item, _) => audit = item)
            .Returns(Task.CompletedTask);
        var service = CreateService(identityRepository, vinculoRepository);

        await service.DeactivateForOficinaAsync(
            42,
            7,
            new SecurityAuditContext(TipoAtorAuditoria.Oficina, 7, "127.0.0.1", "test"));

        vinculoRepository.Verify(repository => repository.DeactivateByOficinaAsync(
            7,
            42,
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
        vinculoRepository.Verify(repository => repository.DeactivateAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
        Assert.NotNull(audit);
        Assert.Equal(TipoAtorAuditoria.Oficina, audit!.TipoAtor);
        Assert.Equal(TipoEventoAuditoria.VinculoRevogado, audit.Evento);
    }

    [Fact]
    public async Task DeactivateForOficinaAsync_DeveFalhar_QuandoVinculoNaoExiste()
    {
        var identityRepository = CreateBooleanTransactionalIdentityRepository();
        var vinculoRepository = new Mock<IClienteOficinaRepository>();
        vinculoRepository
            .Setup(repository => repository.DeactivateByOficinaAsync(
                7,
                42,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var service = CreateService(identityRepository, vinculoRepository);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeactivateForOficinaAsync(
            42,
            7,
            new SecurityAuditContext(TipoAtorAuditoria.Oficina, 7, "127.0.0.1", "test")));

        identityRepository.Verify(repository => repository.AddAuditoriaAsync(
            It.IsAny<AuditoriaSeguranca>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatusForOficinaAsync_DeveReativarVinculoDesativadoPelaOficina()
    {
        var identityRepository = CreateBooleanTransactionalIdentityRepository();
        var vinculoRepository = new Mock<IClienteOficinaRepository>();
        var relacionamento = new ClienteOficina
        {
            OficinaId = 7,
            ClienteId = 42,
            Ativo = false,
            RevogadoEm = null,
            Cliente = new Cliente { Id = 42, Situacao = Situacao.ATIVO }
        };
        vinculoRepository
            .Setup(repository => repository.GetAsync(7, 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(relacionamento);
        vinculoRepository
            .Setup(repository => repository.AddOrActivateAsync(7, 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                relacionamento.Ativo = true;
                return relacionamento;
            });
        var service = CreateService(identityRepository, vinculoRepository);

        var result = await service.UpdateStatusForOficinaAsync(
            42,
            7,
            true,
            new SecurityAuditContext(TipoAtorAuditoria.Oficina, 9, "127.0.0.1", "test"));

        Assert.True(result);
        vinculoRepository.Verify(repository => repository.AddOrActivateAsync(
            7,
            42,
            It.IsAny<CancellationToken>()), Times.Once);
        identityRepository.Verify(repository => repository.AddAuditoriaAsync(
            It.Is<AuditoriaSeguranca>(audit =>
                audit.Evento == TipoEventoAuditoria.VinculoAtivado &&
                audit.ClienteId == 42),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusForOficinaAsync_DeveBloquearReativacaoRevogadaPeloCliente()
    {
        var identityRepository = CreateBooleanTransactionalIdentityRepository();
        var vinculoRepository = new Mock<IClienteOficinaRepository>();
        vinculoRepository
            .Setup(repository => repository.GetAsync(7, 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClienteOficina
            {
                OficinaId = 7,
                ClienteId = 42,
                Ativo = false,
                RevogadoEm = DateTime.UtcNow,
                Cliente = new Cliente { Id = 42, Situacao = Situacao.ATIVO }
            });
        vinculoRepository
            .Setup(repository => repository.AddOrActivateAsync(7, 42, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException(
                "O cliente revogou este vinculo. A oficina nao pode reativa-lo automaticamente."));
        var service = CreateService(identityRepository, vinculoRepository);

        await Assert.ThrowsAsync<ConflictException>(() => service.UpdateStatusForOficinaAsync(
            42,
            7,
            true,
            new SecurityAuditContext(TipoAtorAuditoria.Oficina, 9, "127.0.0.1", "test")));

        identityRepository.Verify(repository => repository.AddAuditoriaAsync(
            It.IsAny<AuditoriaSeguranca>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatusForOficinaAsync_DeveDesativarVinculoAtivo()
    {
        var identityRepository = CreateBooleanTransactionalIdentityRepository();
        var vinculoRepository = new Mock<IClienteOficinaRepository>();
        vinculoRepository
            .Setup(repository => repository.GetAsync(7, 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClienteOficina
            {
                OficinaId = 7,
                ClienteId = 42,
                Ativo = true
            });
        vinculoRepository
            .Setup(repository => repository.DeactivateByOficinaAsync(
                7,
                42,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = CreateService(identityRepository, vinculoRepository);

        var result = await service.UpdateStatusForOficinaAsync(
            42,
            7,
            false,
            new SecurityAuditContext(TipoAtorAuditoria.Oficina, 9, "127.0.0.1", "test"));

        Assert.False(result);
        vinculoRepository.Verify(repository => repository.DeactivateByOficinaAsync(
            7,
            42,
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
        identityRepository.Verify(repository => repository.AddAuditoriaAsync(
            It.Is<AuditoriaSeguranca>(audit =>
                audit.Evento == TipoEventoAuditoria.VinculoRevogado &&
                audit.ClienteId == 42),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Mock<IClienteIdentityRepository> CreateTransactionalIdentityRepository()
    {
        var repository = new Mock<IClienteIdentityRepository>();
        repository
            .Setup(item => item.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<PreCadastroClienteResultadoDTO>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<PreCadastroClienteResultadoDTO>>, CancellationToken>(
                (operation, token) => operation(token));
        return repository;
    }

    private static Mock<IClienteIdentityRepository> CreateBooleanTransactionalIdentityRepository()
    {
        var repository = new Mock<IClienteIdentityRepository>();
        repository
            .Setup(item => item.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<bool>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<bool>>, CancellationToken>(
                (operation, token) => operation(token));
        return repository;
    }

    private static Mock<IClienteOficinaRepository> CreateLinkRepository(
        Cliente cliente,
        Mock<IClienteIdentityRepository> identityRepository)
    {
        identityRepository
            .Setup(repository => repository.GetClienteByCpfCnpjAsync(
                new string(cliente.Cpf_Cnpj.Where(char.IsDigit).ToArray()),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliente);
        var vinculoRepository = new Mock<IClienteOficinaRepository>();
        vinculoRepository
            .Setup(repository => repository.AddOrActivateAsync(7, cliente.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClienteOficina
            {
                OficinaId = 7,
                ClienteId = cliente.Id,
                Ativo = true
            });
        return vinculoRepository;
    }

    private static PreCadastrarClienteDTO CreateFullPreRegistration() => new()
    {
        Cpf = "529.982.247-25",
        Nome = "Cliente Completo",
        Email = "CLIENTE@EXAMPLE.COM",
        Obs = " Observação da oficina ",
        Razao = "",
        DataNasc = new DateOnly(1950, 5, 10),
        Sexo = Sexo.Feminino,
        Numero = 120,
        Rua = " Rua das Flores ",
        Cidade = "Blumenau",
        Cep = "89010-000",
        Bairro = "Centro",
        Estado = "SC",
        Pais = "Brasil",
        Complemento = "Casa",
        Telefones = new[]
        {
            new PreCadastrarTelefoneClienteDTO { DDD = 47, Numero = "99999-9999" }
        }
    };

    private static ClienteVinculoService CreateService(
        Mock<IClienteIdentityRepository> identityRepository,
        Mock<IClienteOficinaRepository> vinculoRepository)
    {
        return new ClienteVinculoService(
            identityRepository.Object,
            vinculoRepository.Object,
            new CpfCnpjValidator(new CpfValidator(), new CnpjValidator()),
            TimeProvider.System);
    }
}
