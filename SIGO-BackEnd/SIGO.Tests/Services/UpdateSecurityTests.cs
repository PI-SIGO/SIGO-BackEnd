using AutoMapper;
using Moq;
using SIGO.Data.Interfaces;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;
using SIGO.Security;
using SIGO.Services.Entities;
using SIGO.Services.Interfaces;
using SIGO.Validation;
using Xunit;

namespace SIGO.Tests.Services
{
    public class UpdateSecurityTests
    {
        [Fact]
        public async Task ClienteUpdate_NaoDeveSobrescreverSenha_QuandoSenhaNaoFoiEnviada()
        {
            var cliente = new Cliente
            {
                Id = 1,
                Nome = "Cliente",
                Email = "cliente@test.com",
                Senha = "hash-original",
                Cpf_Cnpj = "52998224725",
                Cep = "12345678"
            };
            var clienteRepository = new Mock<IClienteRepository>();
            clienteRepository.Setup(r => r.GetById(1)).ReturnsAsync(cliente);
            clienteRepository.Setup(r => r.SaveChanges()).ReturnsAsync(1);
            var service = CreateClienteService(clienteRepository.Object);

            await service.Update(new ClienteRequestDTO
            {
                Nome = "Cliente Atualizado",
                Email = "cliente@test.com",
                Cpf_Cnpj = "52998224725",
                Cep = "12345678",
                senha = null
            }, 1);

            Assert.Equal("hash-original", cliente.Senha);
            Assert.Equal("Cliente Atualizado", cliente.Nome);
        }

        [Fact]
        public async Task ClienteUpdate_DeveRejeitarTelefoneDeOutroCliente_AntesDeSalvarCliente()
        {
            var cliente = new Cliente
            {
                Id = 1,
                Nome = "Cliente",
                Email = "cliente@test.com",
                Senha = "hash-original",
                Cpf_Cnpj = "52998224725",
                Cep = "12345678"
            };
            var clienteRepository = new Mock<IClienteRepository>();
            clienteRepository.Setup(r => r.GetById(1)).ReturnsAsync(cliente);

            var telefoneRepository = new Mock<ITelefoneRepository>();
            telefoneRepository
                .Setup(r => r.GetInvalidIdsForCliente(
                    1,
                    It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { 42 }))))
                .ReturnsAsync((IReadOnlyCollection<int>)new List<int> { 42 });

            var service = CreateClienteService(clienteRepository.Object, telefoneRepository.Object);

            var exception = await Assert.ThrowsAsync<BusinessValidationException>(() => service.Update(new ClienteRequestDTO
            {
                Nome = "Cliente Atualizado",
                Email = "cliente@test.com",
                Cpf_Cnpj = "52998224725",
                Cep = "12345678",
                senha = null,
                Telefones = new List<TelefoneDTO>
                {
                    new()
                    {
                        Id = 42,
                        DDD = 11,
                        Numero = "999999999",
                        ClienteId = 2
                    }
                }
            }, 1));

            Assert.Contains(exception.Errors, error =>
                error.Field == nameof(ClienteDTO.Telefones) &&
                error.Message.Contains("42"));
            Assert.Equal("Cliente", cliente.Nome);
            clienteRepository.Verify(r => r.SaveChanges(), Times.Never);
            telefoneRepository.Verify(r => r.UpdateForCliente(It.IsAny<Telefone>(), It.IsAny<int>()), Times.Never);
            telefoneRepository.Verify(r => r.Update(It.IsAny<Telefone>()), Times.Never);
            telefoneRepository.Verify(r => r.Add(It.IsAny<Telefone>()), Times.Never);
        }

        [Fact]
        public async Task ClienteUpdate_DeveAtualizarTelefoneSomenteQuandoPertenceAoCliente()
        {
            var cliente = new Cliente
            {
                Id = 1,
                Nome = "Cliente",
                Email = "cliente@test.com",
                Senha = "hash-original",
                Cpf_Cnpj = "52998224725",
                Cep = "12345678"
            };
            var clienteRepository = new Mock<IClienteRepository>();
            clienteRepository.Setup(r => r.GetById(1)).ReturnsAsync(cliente);
            clienteRepository.Setup(r => r.SaveChanges()).ReturnsAsync(1);

            var telefoneRepository = new Mock<ITelefoneRepository>();
            telefoneRepository
                .Setup(r => r.GetInvalidIdsForCliente(
                    1,
                    It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { 42 }))))
                .ReturnsAsync(Array.Empty<int>());
            telefoneRepository
                .Setup(r => r.UpdateForCliente(
                    It.Is<Telefone>(t =>
                        t.Id == 42 &&
                        t.ClienteId == 1 &&
                        t.DDD == 11 &&
                        t.Numero == "988887777"),
                    1))
                .ReturnsAsync(true);

            var mapper = new Mock<IMapper>();
            mapper
                .Setup(m => m.Map<Telefone>(It.IsAny<TelefoneDTO>()))
                .Returns<TelefoneDTO>(dto => new Telefone
                {
                    Id = dto.Id,
                    ClienteId = dto.ClienteId,
                    DDD = dto.DDD,
                    Numero = dto.Numero
                });
            var service = CreateClienteService(clienteRepository.Object, telefoneRepository.Object, mapper.Object);

            await service.Update(new ClienteRequestDTO
            {
                Nome = "Cliente Atualizado",
                Email = "cliente@test.com",
                Cpf_Cnpj = "52998224725",
                Cep = "12345678",
                senha = null,
                Telefones = new List<TelefoneDTO>
                {
                    new()
                    {
                        Id = 42,
                        DDD = 11,
                        Numero = "988887777",
                        ClienteId = 99
                    }
                }
            }, 1);

            Assert.Equal("Cliente Atualizado", cliente.Nome);
            telefoneRepository.Verify(r => r.UpdateForCliente(It.IsAny<Telefone>(), 1), Times.Once);
            telefoneRepository.Verify(r => r.Update(It.IsAny<Telefone>()), Times.Never);
        }

        [Fact]
        public async Task ClienteUpdate_DeveRejeitarTelefoneQuandoRepositorioNaoConfirmaDono()
        {
            var cliente = new Cliente
            {
                Id = 1,
                Nome = "Cliente",
                Email = "cliente@test.com",
                Senha = "hash-original",
                Cpf_Cnpj = "52998224725",
                Cep = "12345678"
            };
            var clienteRepository = new Mock<IClienteRepository>();
            clienteRepository.Setup(r => r.GetById(1)).ReturnsAsync(cliente);
            clienteRepository.Setup(r => r.SaveChanges()).ReturnsAsync(1);

            var telefoneRepository = new Mock<ITelefoneRepository>();
            telefoneRepository
                .Setup(r => r.GetInvalidIdsForCliente(
                    1,
                    It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { 42 }))))
                .ReturnsAsync(Array.Empty<int>());
            telefoneRepository
                .Setup(r => r.UpdateForCliente(It.Is<Telefone>(t => t.Id == 42), 1))
                .ReturnsAsync(false);

            var mapper = new Mock<IMapper>();
            mapper
                .Setup(m => m.Map<Telefone>(It.IsAny<TelefoneDTO>()))
                .Returns<TelefoneDTO>(dto => new Telefone
                {
                    Id = dto.Id,
                    ClienteId = dto.ClienteId,
                    DDD = dto.DDD,
                    Numero = dto.Numero
                });
            var service = CreateClienteService(clienteRepository.Object, telefoneRepository.Object, mapper.Object);

            var exception = await Assert.ThrowsAsync<BusinessValidationException>(() => service.Update(new ClienteRequestDTO
            {
                Nome = "Cliente Atualizado",
                Email = "cliente@test.com",
                Cpf_Cnpj = "52998224725",
                Cep = "12345678",
                senha = null,
                Telefones = new List<TelefoneDTO>
                {
                    new()
                    {
                        Id = 42,
                        DDD = 11,
                        Numero = "988887777",
                        ClienteId = 1
                    }
                }
            }, 1));

            Assert.Contains(exception.Errors, error =>
                error.Field == nameof(ClienteDTO.Telefones) &&
                error.Message.Contains("42"));
            telefoneRepository.Verify(r => r.Update(It.IsAny<Telefone>()), Times.Never);
            telefoneRepository.Verify(r => r.Add(It.IsAny<Telefone>()), Times.Never);
        }

        [Fact]
        public async Task PedidoUpdate_DevePreservarOficina_QuandoDtoNaoEnviaIdOficina()
        {
            var pedido = new Pedido { Id = 5, idOficina = 9, idCliente = 1, idFuncionario = 2, idVeiculo = 3 };
            var repository = new Mock<IPedidoRepository>();
            repository.Setup(r => r.GetById(5)).ReturnsAsync(pedido);
            repository.Setup(r => r.SaveChanges()).ReturnsAsync(1);
            var service = new PedidoService(repository.Object, Mock.Of<IMapper>());

            await service.Update(new PedidoDTO { idOficina = 0, Observacao = "Atualizado" }, 5);

            Assert.Equal(9, pedido.idOficina);
            Assert.Equal("Atualizado", pedido.Observacao);
        }

        [Fact]
        public async Task VeiculoUpdateForOficina_DevePreservarClienteId_QuandoDtoNaoEnviaClienteId()
        {
            var veiculo = new Veiculo
            {
                Id = 2,
                ClienteId = 5,
                NomeVeiculo = "Original",
                TipoVeiculo = "Hatch",
                PlacaVeiculo = "ABC1234",
                ChassiVeiculo = "CHASSI123",
                AnoFab = 2020,
                Quilometragem = 1000,
                Combustivel = "Gasolina",
                Seguro = "Ativo",
                Cor = "Preto"
            };
            var veiculoRepository = new Mock<IVeiculoRepository>();
            veiculoRepository.Setup(r => r.GetByIdForOficina(2, 7)).ReturnsAsync(veiculo);
            veiculoRepository.Setup(r => r.SaveChanges()).ReturnsAsync(1);

            var clienteRepository = new Mock<IClienteRepository>();
            clienteRepository.Setup(r => r.ExistsInOficina(5, 7)).ReturnsAsync(true);
            var service = CreateVeiculoService(veiculoRepository.Object, clienteRepository.Object);

            await service.UpdateVeiculoForOficina(new VeiculoDTO
            {
                ClienteId = 0,
                NomeVeiculo = "Atualizado",
                TipoVeiculo = "Sedan",
                PlacaVeiculo = "DEF5678",
                ChassiVeiculo = "CHASSI456",
                AnoFab = 2021,
                Quilometragem = 2000,
                Combustivel = "Flex",
                Seguro = "Ativo",
                Cor = "Branco"
            }, 2, 7);

            Assert.Equal(5, veiculo.ClienteId);
            Assert.Equal("Atualizado", veiculo.NomeVeiculo);
            clienteRepository.Verify(r => r.ExistsInOficina(5, 7), Times.Once);
            veiculoRepository.Verify(r => r.SaveChanges(), Times.Once);
        }

        [Fact]
        public async Task VeiculoUpdateForOficina_DeveRejeitarTrocaDeCliente()
        {
            var veiculo = new Veiculo { Id = 2, ClienteId = 5 };
            var veiculoRepository = new Mock<IVeiculoRepository>();
            veiculoRepository.Setup(r => r.GetByIdForOficina(2, 7)).ReturnsAsync(veiculo);
            var clienteRepository = new Mock<IClienteRepository>();
            var service = CreateVeiculoService(veiculoRepository.Object, clienteRepository.Object);

            var exception = await Assert.ThrowsAsync<BusinessValidationException>(() => service.UpdateVeiculoForOficina(
                new VeiculoDTO
                {
                    ClienteId = 99,
                    NomeVeiculo = "Atualizado"
                },
                2,
                7));

            Assert.Contains(exception.Errors, error =>
                error.Field == nameof(VeiculoDTO.ClienteId) &&
                error.Message == "Cliente do veiculo nao pode ser alterado.");
            Assert.Equal(5, veiculo.ClienteId);
            clienteRepository.Verify(r => r.ExistsInOficina(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            veiculoRepository.Verify(r => r.SaveChanges(), Times.Never);
        }

        private static ClienteService CreateClienteService(
            IClienteRepository clienteRepository,
            ITelefoneRepository telefoneRepository = null,
            IMapper mapper = null,
            IPasswordHasher passwordHasher = null)
        {
            var cpfValidator = new CpfValidator();
            var cnpjValidator = new CnpjValidator();
            var cpfCnpjValidator = new CpfCnpjValidator(cpfValidator, cnpjValidator);

            return new ClienteService(
                clienteRepository,
                telefoneRepository ?? Mock.Of<ITelefoneRepository>(),
                mapper ?? Mock.Of<IMapper>(),
                cpfCnpjValidator,
                Mock.Of<IClienteOficinaRepository>(),
                null,
                passwordHasher ?? Mock.Of<IPasswordHasher>());
        }

        private static VeiculoService CreateVeiculoService(
            IVeiculoRepository veiculoRepository,
            IClienteRepository clienteRepository)
        {
            return new VeiculoService(
                veiculoRepository,
                Mock.Of<IMapper>(),
                clienteRepository,
                Mock.Of<IVeiculoImagemStorageService>());
        }
    }
}
