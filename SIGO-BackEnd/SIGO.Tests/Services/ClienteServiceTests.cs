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
