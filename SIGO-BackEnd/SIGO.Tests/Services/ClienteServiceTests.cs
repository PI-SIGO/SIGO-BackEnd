using AutoMapper;
using Moq;
using SIGO.Data.Interfaces;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Enums;
using SIGO.Objects.Models;
using SIGO.Services.Entities;
using SIGO.Validation;
using Xunit;

namespace SIGO.Tests.Services
{
    public class ClienteServiceTests
    {
        private readonly Mock<IClienteRepository> _clienteRepositoryMock = new();
        private readonly Mock<ITelefoneRepository> _telefoneRepositoryMock = new();
        private readonly Mock<IMapper> _mapperMock = new();

        [Fact]
        public async Task GetByIdWithDetailsForOficina_DeveRetornarDadosCompletos_QuandoVinculoEstaAtivo()
        {
            var service = CreateService();
            var cliente = new Cliente
            {
                Id = 7,
                Nome = "Cliente Permitido",
                Email = "cliente@test.com",
                Cpf_Cnpj = "12345678901",
                Rua = "Rua Privada",
                ClienteOficinas = new List<ClienteOficina>
                {
                    new()
                    {
                        OficinaId = 2,
                        ClienteId = 7,
                        Ativo = true
                    }
                },
                Telefones = new List<Telefone>
                {
                    new() { Id = 1, DDD = 11, Numero = "999999999", ClienteId = 7 }
                },
                Veiculos = new List<Veiculo>
                {
                    new() { Id = 1, ClienteId = 7, NomeVeiculo = "Carro" }
                }
            };

            _clienteRepositoryMock
                .Setup(repository => repository.GetByIdWithDetailsForOficina(7, 2))
                .ReturnsAsync(cliente);
            _mapperMock
                .Setup(mapper => mapper.Map<List<TelefoneDTO>>(cliente.Telefones))
                .Returns(new List<TelefoneDTO>
                {
                    new() { Id = 1, DDD = 11, Numero = "999999999", ClienteId = 7 }
                });
            _mapperMock
                .Setup(mapper => mapper.Map<List<VeiculoDTO>>(cliente.Veiculos))
                .Returns(new List<VeiculoDTO>
                {
                    new() { Id = 1, ClienteId = 7, NomeVeiculo = "Carro" }
                });

            var result = await service.GetByIdWithDetailsForOficina(7, 2);

            Assert.NotNull(result);
            Assert.Equal(7, result!.Id);
            Assert.Equal("Cliente Permitido", result.Nome);
            Assert.Equal("cliente@test.com", result.Email);
            Assert.Equal("12345678901", result.Cpf_Cnpj);
            Assert.Single(result.Telefones!);
            Assert.Single(result.Veiculos!);
        }

        [Fact]
        public async Task GetByIdWithDetailsForOficina_DeveOcultarDados_QuandoVinculoEstaInativo()
        {
            var service = CreateService();
            var cliente = new Cliente
            {
                Id = 7,
                Nome = "Cliente Pendente",
                ClienteOficinas = new List<ClienteOficina>
                {
                    new()
                    {
                        OficinaId = 2,
                        ClienteId = 7,
                        Ativo = false
                    }
                }
            };

            _clienteRepositoryMock
                .Setup(repository => repository.GetByIdWithDetailsForOficina(7, 2))
                .ReturnsAsync(cliente);

            var result = await service.GetByIdWithDetailsForOficina(7, 2);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByOficina_DeveDiferenciarCadastroAtivoDeVinculoInativo()
        {
            var cliente = new Cliente
            {
                Id = 7,
                Nome = "Cliente inativo",
                Situacao = Situacao.ATIVO,
                ClienteOficinas = new List<ClienteOficina>
                {
                    new()
                    {
                        ClienteId = 7,
                        OficinaId = 2,
                        Ativo = false,
                        RevogadoEm = DateTime.UtcNow
                    }
                }
            };
            _clienteRepositoryMock
                .Setup(repository => repository.GetByOficina(2))
                .ReturnsAsync(new[] { cliente });
            _mapperMock
                .Setup(mapper => mapper.Map<List<TelefoneDTO>>(cliente.Telefones))
                .Returns(new List<TelefoneDTO>());
            _mapperMock
                .Setup(mapper => mapper.Map<List<VeiculoDTO>>(cliente.Veiculos))
                .Returns(new List<VeiculoDTO>());
            var service = CreateService();

            var result = await service.GetByOficina(2);

            var returnedClient = Assert.Single(result);
            Assert.Equal(7, returnedClient.Id);
            Assert.Equal((int)Situacao.ATIVO, returnedClient.Situacao);
            Assert.False(returnedClient.VinculoAtivo);
        }

        [Fact]
        public async Task GetByIdWithDetailsForOficina_DeveOcultarHistoricosDeOutrasOficinas()
        {
            var service = CreateService();
            var vehicle = new Veiculo { Id = 11, ClienteId = 7 };
            var cliente = new Cliente
            {
                Id = 7,
                Situacao = Situacao.ATIVO,
                ClienteOficinas = new List<ClienteOficina>
                {
                    new() { OficinaId = 2, ClienteId = 7, Ativo = true }
                },
                Veiculos = new List<Veiculo> { vehicle }
            };
            _clienteRepositoryMock
                .Setup(repository => repository.GetByIdWithDetailsForOficina(7, 2))
                .ReturnsAsync(cliente);
            _mapperMock
                .Setup(mapper => mapper.Map<List<TelefoneDTO>>(cliente.Telefones))
                .Returns(new List<TelefoneDTO>());
            _mapperMock
                .Setup(mapper => mapper.Map<List<VeiculoDTO>>(cliente.Veiculos))
                .Returns(new List<VeiculoDTO>
                {
                    new()
                    {
                        Id = 11,
                        ClienteId = 7,
                        Pedidos = new List<PedidoDTO>
                        {
                            new() { Id = 20, idOficina = 2 },
                            new() { Id = 30, idOficina = 3 }
                        },
                        RegistroServicos = new List<RegistroServicoDTO>
                        {
                            new() { Id = 40, OficinaId = 2 },
                            new() { Id = 50, OficinaId = 3 }
                        }
                    }
                });

            var result = await service.GetByIdWithDetailsForOficina(7, 2);

            var returnedVehicle = Assert.Single(result!.Veiculos);
            Assert.Equal(20, Assert.Single(returnedVehicle.Pedidos).Id);
            Assert.Equal(40, Assert.Single(returnedVehicle.RegistroServicos).Id);
        }

        [Fact]
        public async Task DeactivateAsync_DeveDelegarInativacaoLogicaAoRepositorio()
        {
            _clienteRepositoryMock
                .Setup(repository => repository.DeactivateAsync(7, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            var service = CreateService();

            var result = await service.DeactivateAsync(7);

            Assert.True(result);
            _clienteRepositoryMock.Verify(repository => repository.DeactivateAsync(
                7,
                It.IsAny<CancellationToken>()), Times.Once);
            _clienteRepositoryMock.Verify(repository => repository.Remove(It.IsAny<Cliente>()), Times.Never);
        }

        [Fact]
        public async Task UpdateForOficina_DeveAtualizarSomenteClienteComVinculoAtivo()
        {
            var cliente = CreateLinkedCliente();
            var request = CreateUpdateRequest();
            _clienteRepositoryMock
                .Setup(repository => repository.GetByIdWithDetailsForOficina(7, 2))
                .ReturnsAsync(cliente);
            var service = CreateService();

            var result = await service.UpdateForOficina(request, 7, 2);

            Assert.Equal("Nome atualizado", cliente.Nome);
            Assert.Equal("Nome atualizado", result.Nome);
            _clienteRepositoryMock.Verify(repository => repository.SaveChanges(), Times.Once);
        }

        [Fact]
        public async Task UpdateForOficina_DeveLimparRazaoSocial_QuandoClienteForPessoaFisica()
        {
            var cliente = CreateLinkedCliente();
            var request = CreateUpdateRequest();
            request.Obs = "Observação válida";
            request.razao = "Razão que não se aplica";
            _clienteRepositoryMock
                .Setup(repository => repository.GetByIdWithDetailsForOficina(7, 2))
                .ReturnsAsync(cliente);
            var service = CreateService();

            await service.UpdateForOficina(request, 7, 2);

            Assert.Equal(TipoCliente.FISICO, cliente.TipoCliente);
            Assert.Equal("Observação válida", cliente.Obs);
            Assert.Equal(string.Empty, cliente.Razao);
        }

        [Fact]
        public async Task UpdateForOficina_DeveLimparObservacao_QuandoClienteForPessoaJuridica()
        {
            var cliente = CreateLinkedCliente();
            cliente.Cpf_Cnpj = "11222333000181";
            cliente.TipoCliente = TipoCliente.JURIDICO;
            var request = CreateUpdateRequest();
            request.Cpf_Cnpj = "11.222.333/0001-81";
            request.Obs = "Observação que não se aplica";
            request.razao = "Empresa SIGO Ltda.";
            request.DataNasc = null;
            request.Sexo = null;
            _clienteRepositoryMock
                .Setup(repository => repository.GetByIdWithDetailsForOficina(7, 2))
                .ReturnsAsync(cliente);
            var service = CreateService();

            await service.UpdateForOficina(request, 7, 2);

            Assert.Equal(TipoCliente.JURIDICO, cliente.TipoCliente);
            Assert.Equal(string.Empty, cliente.Obs);
            Assert.Equal("Empresa SIGO Ltda.", cliente.Razao);
            Assert.Null(cliente.DataNasc);
            Assert.Equal(Sexo.Outro, cliente.Sexo);
        }

        [Fact]
        public async Task UpdateForOficina_DeveExigirRazaoSocial_QuandoClienteForPessoaJuridica()
        {
            var cliente = CreateLinkedCliente();
            cliente.Cpf_Cnpj = "11222333000181";
            cliente.TipoCliente = TipoCliente.JURIDICO;
            var request = CreateUpdateRequest();
            request.Cpf_Cnpj = "11.222.333/0001-81";
            request.razao = string.Empty;
            _clienteRepositoryMock
                .Setup(repository => repository.GetByIdWithDetailsForOficina(7, 2))
                .ReturnsAsync(cliente);
            var service = CreateService();

            var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
                service.UpdateForOficina(request, 7, 2));

            Assert.Contains(
                exception.Errors,
                error => error.Field == nameof(ClienteDTO.razao));
            _clienteRepositoryMock.Verify(repository => repository.SaveChanges(), Times.Never);
        }

        [Fact]
        public async Task UpdateForOficina_DeveFalhar_QuandoClienteNaoTemVinculoAtivo()
        {
            _clienteRepositoryMock
                .Setup(repository => repository.GetByIdWithDetailsForOficina(7, 2))
                .ReturnsAsync((Cliente?)null);
            var service = CreateService();

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.UpdateForOficina(CreateUpdateRequest(), 7, 2));

            _clienteRepositoryMock.Verify(repository => repository.SaveChanges(), Times.Never);
        }

        private static Cliente CreateLinkedCliente() => new()
        {
            Id = 7,
            Nome = "Nome anterior",
            Email = "cliente@test.com",
            Cpf_Cnpj = "52998224725",
            Cep = "89010000",
            Situacao = Situacao.ATIVO,
            ClienteOficinas = new List<ClienteOficina>
            {
                new() { ClienteId = 7, OficinaId = 2, Ativo = true }
            },
            Telefones = new List<Telefone>(),
            Veiculos = new List<Veiculo>()
        };

        private static ClienteRequestDTO CreateUpdateRequest() => new()
        {
            Nome = "Nome atualizado",
            Email = "cliente@test.com",
            Cpf_Cnpj = "529.982.247-25",
            Obs = string.Empty,
            razao = string.Empty,
            Cep = "89010000",
            Rua = "Rua Atualizada",
            Cidade = "Blumenau",
            Bairro = "Centro",
            Estado = "SC",
            Pais = "Brasil",
            Complemento = string.Empty,
            DataNasc = new DateOnly(1990, 5, 10),
            Sexo = (int)Sexo.Feminino,
            senha = string.Empty,
            Telefones = new List<TelefoneDTO>()
        };

        private ClienteService CreateService()
        {
            var cpfCnpjValidator = new CpfCnpjValidator(new CpfValidator(), new CnpjValidator());

            return new ClienteService(
                _clienteRepositoryMock.Object,
                _telefoneRepositoryMock.Object,
                _mapperMock.Object,
                cpfCnpjValidator,
                null);
        }
    }
}
