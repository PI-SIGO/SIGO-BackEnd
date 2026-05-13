using AutoMapper;
using Moq;
using SIGO.Data.Interfaces;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;
using SIGO.Security;
using SIGO.Services.Entities;
using SIGO.Utils;
using SIGO.Validation;
using Xunit;

namespace SIGO.Tests.Services
{
    public class ClienteServiceTests
    {
        private readonly Mock<IClienteRepository> _clienteRepositoryMock = new();
        private readonly Mock<IClienteOficinaRepository> _clienteOficinaRepositoryMock = new();
        private readonly Mock<ITelefoneRepository> _telefoneRepositoryMock = new();
        private readonly Mock<IMapper> _mapperMock = new();
        private readonly Mock<IPasswordHasher> _passwordHasherMock = new();

        [Fact]
        public async Task Create_DeveRetornarTodosErros_QuandoCpfCnpjECepForemInvalidos()
        {
            var service = CreateService();
            var dto = new ClienteRequestDTO
            {
                Nome = "Cliente",
                Email = "cliente@test.com",
                senha = "123",
                Cpf_Cnpj = "111",
                Cep = "123"
            };

            var exception = await Assert.ThrowsAsync<BusinessValidationException>(() => service.Create(dto));

            Assert.Contains(exception.Errors, error =>
                error.Field == nameof(ClienteDTO.Cpf_Cnpj) &&
                error.Message == "CPF/CNPJ inválido.");
            Assert.Contains(exception.Errors, error =>
                error.Field == nameof(ClienteDTO.Cep) &&
                error.Message == "CEP inválido. O CEP deve conter 8 dígitos.");
            Assert.Equal(2, exception.Errors.Count);
            _clienteRepositoryMock.Verify(r => r.Add(It.IsAny<SIGO.Objects.Models.Cliente>()), Times.Never);
        }

        [Fact]
        public async Task GetByIdWithDetailsForOficina_DeveRetornarSomenteCamposPermitidos()
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
                        Ativo = true,
                        DadosPermitidos = ClienteCompartilhamentoCampos.Serializar(new[] { ClienteCompartilhamentoCampos.Nome })
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

            _clienteRepositoryMock.Setup(r => r.GetByIdWithDetailsForOficina(7, 2)).ReturnsAsync(cliente);

            var result = await service.GetByIdWithDetailsForOficina(7, 2);

            Assert.NotNull(result);
            Assert.Equal(7, result!.Id);
            Assert.Equal("Cliente Permitido", result.Nome);
            Assert.Null(result.Email);
            Assert.Null(result.Cpf_Cnpj);
            Assert.Null(result.Telefones);
            Assert.Null(result.Veiculos);
        }

        [Fact]
        public async Task CreateForOficina_DeveVincularClienteExistente_QuandoCpfCnpjEEmailConferem()
        {
            var service = CreateService();
            var dto = CriarClienteDto();
            var clienteExistente = new Cliente
            {
                Id = 10,
                Nome = dto.Nome,
                Email = dto.Email,
                Cpf_Cnpj = dto.Cpf_Cnpj
            };

            _clienteRepositoryMock
                .Setup(r => r.GetByCpfCnpjOrEmail(dto.Cpf_Cnpj, dto.Email))
                .ReturnsAsync(new List<Cliente> { clienteExistente });
            _mapperMock
                .Setup(m => m.Map<ClienteDTO>(clienteExistente))
                .Returns(new ClienteDTO { Id = 10, Nome = dto.Nome, Email = dto.Email, Cpf_Cnpj = dto.Cpf_Cnpj });
            _clienteOficinaRepositoryMock
                .Setup(r => r.AddOrUpdatePermissoesAsync(3, 10, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var result = await service.CreateForOficina(dto, 3);

            Assert.Equal(10, result.Id);
            _clienteRepositoryMock.Verify(r => r.Add(It.IsAny<Cliente>()), Times.Never);
            _clienteOficinaRepositoryMock.Verify(r => r.AddOrUpdatePermissoesAsync(
                3,
                10,
                It.Is<string>(dados =>
                    dados.Contains(ClienteCompartilhamentoCampos.Nome) &&
                    dados.Contains(ClienteCompartilhamentoCampos.Email) &&
                    dados.Contains(ClienteCompartilhamentoCampos.CpfCnpj) &&
                    dados.Contains(ClienteCompartilhamentoCampos.Telefones) &&
                    dados.Contains(ClienteCompartilhamentoCampos.Veiculos))),
                Times.Once);
        }

        [Fact]
        public async Task CreateForOficina_DeveRejeitarVinculo_QuandoCpfCnpjEEmailNaoPertencemAoMesmoCliente()
        {
            var service = CreateService();
            var dto = CriarClienteDto();
            var clienteExistente = new Cliente
            {
                Id = 10,
                Nome = "Outro Cliente",
                Email = "outro@test.com",
                Cpf_Cnpj = dto.Cpf_Cnpj
            };

            _clienteRepositoryMock
                .Setup(r => r.GetByCpfCnpjOrEmail(dto.Cpf_Cnpj, dto.Email))
                .ReturnsAsync(new List<Cliente> { clienteExistente });

            await Assert.ThrowsAsync<BusinessValidationException>(() => service.CreateForOficina(dto, 3));

            _clienteOficinaRepositoryMock.Verify(r => r.AddOrUpdatePermissoesAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>()),
                Times.Never);
        }

        private ClienteService CreateService()
        {
            var cpfValidator = new CpfValidator();
            var cnpjValidator = new CnpjValidator();
            var cpfCnpjValidator = new CpfCnpjValidator(cpfValidator, cnpjValidator);

            return new ClienteService(
                _clienteRepositoryMock.Object,
                _telefoneRepositoryMock.Object,
                _mapperMock.Object,
                cpfCnpjValidator,
                _clienteOficinaRepositoryMock.Object,
                null,
                _passwordHasherMock.Object);
        }

        private static ClienteRequestDTO CriarClienteDto()
        {
            return new ClienteRequestDTO
            {
                Nome = "Cliente",
                Email = "cliente@test.com",
                senha = "123",
                Cpf_Cnpj = "52998224725",
                Cep = "12345678",
                Rua = "Rua A",
                Cidade = "Cidade",
                Bairro = "Centro",
                Estado = "SP",
                Pais = "Brasil",
                Complemento = string.Empty
            };
        }
    }
}
