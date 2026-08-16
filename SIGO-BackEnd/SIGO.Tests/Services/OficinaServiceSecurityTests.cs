using AutoMapper;
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
    public sealed class OficinaServiceSecurityTests
    {
        private readonly Mock<IOficinaRepository> _repository = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ICnpjValidator> _cnpjValidator = new();
        private readonly Mock<IPasswordHasher> _passwordHasher = new();

        [Fact]
        public async Task Login_DeveNegarOficinaInativa_MesmoComSenhaCorreta()
        {
            var oficina = new Oficina
            {
                Id = 7,
                Email = "oficina@test.com",
                Senha = "hash",
                Situacao = Situacao.INATIVO
            };
            _repository
                .Setup(repository => repository.GetByEmail("oficina@test.com"))
                .ReturnsAsync(oficina);
            _passwordHasher
                .Setup(hasher => hasher.Verify("Senha123", "hash"))
                .Returns(true);
            var service = CreateService();

            var result = await service.Login(new Login
            {
                Email = "oficina@test.com",
                Password = "Senha123"
            });

            Assert.Null(result);
            _repository.Verify(
                repository => repository.UpdatePasswordHash(It.IsAny<int>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Login_DeveNormalizarMaiusculasEEspacosAntesDaConsulta()
        {
            var oficina = new Oficina
            {
                Id = 7,
                Email = "oficina@test.com",
                Senha = "hash",
                Situacao = Situacao.ATIVO
            };
            var expected = new OficinaDTO { Id = 7, Email = "oficina@test.com" };
            _repository
                .Setup(repository => repository.GetByEmail("oficina@test.com"))
                .ReturnsAsync(oficina);
            _passwordHasher
                .Setup(hasher => hasher.Verify("Senha123", "hash"))
                .Returns(true);
            _mapper
                .Setup(mapper => mapper.Map<OficinaDTO?>(oficina))
                .Returns(expected);

            var result = await CreateService().Login(new Login
            {
                Email = "  OFICINA@Test.COM  ",
                Password = "Senha123"
            });

            Assert.Same(expected, result);
            _repository.Verify(
                repository => repository.GetByEmail("oficina@test.com"),
                Times.Once);
        }

        [Fact]
        public async Task Create_DevePersistirEmailNormalizado()
        {
            var request = new OficinaRequestDTO
            {
                CNPJ = "11222333000181",
                Email = "  OFICINA@EXAMPLE.COM  ",
                Senha = "Senha123",
                Cep = 89010000
            };
            var entity = new Oficina { Id = 11 };
            _cnpjValidator.Setup(validator => validator.IsValid(request.CNPJ)).Returns(true);
            _cnpjValidator.Setup(validator => validator.Normalize(request.CNPJ)).Returns("11222333000181");
            _repository
                .Setup(repository => repository.ExistsByEmail("oficina@example.com", null))
                .ReturnsAsync(false);
            _mapper.Setup(mapper => mapper.Map<Oficina>(request)).Returns(entity);
            _passwordHasher.Setup(hasher => hasher.Hash("Senha123")).Returns("hash");

            await CreateService().Create(request);

            Assert.Equal("oficina@example.com", request.Email);
            _repository.Verify(repository => repository.Add(entity), Times.Once);
        }

        [Fact]
        public async Task Create_DeveRejeitarEmailDuplicadoIgnorandoCaseEEspacos()
        {
            var request = new OficinaRequestDTO
            {
                CNPJ = "11222333000181",
                Email = "  OFICINA@EXAMPLE.COM  ",
                Senha = "Senha123",
                Cep = 89010000
            };
            _cnpjValidator.Setup(validator => validator.IsValid(request.CNPJ)).Returns(true);
            _cnpjValidator.Setup(validator => validator.Normalize(request.CNPJ)).Returns("11222333000181");
            _repository
                .Setup(repository => repository.ExistsByEmail("oficina@example.com", null))
                .ReturnsAsync(true);

            var exception = await Assert.ThrowsAsync<BusinessValidationException>(
                () => CreateService().Create(request));

            Assert.Contains(
                exception.Errors,
                error => error.Field == nameof(OficinaDTO.Email) && error.Message == "E-mail já cadastrado.");
            _repository.Verify(repository => repository.Add(It.IsAny<Oficina>()), Times.Never);
        }

        [Fact]
        public async Task Create_DeveRejeitarCepZero()
        {
            var request = new OficinaRequestDTO
            {
                CNPJ = "11222333000181",
                Email = "oficina@example.com",
                Senha = "Senha123",
                Cep = 0
            };
            _cnpjValidator.Setup(validator => validator.IsValid(request.CNPJ)).Returns(true);
            _cnpjValidator.Setup(validator => validator.Normalize(request.CNPJ)).Returns("11222333000181");
            _repository
                .Setup(repository => repository.ExistsByEmail("oficina@example.com", null))
                .ReturnsAsync(false);

            var exception = await Assert.ThrowsAsync<BusinessValidationException>(
                () => CreateService().Create(request));

            Assert.Contains(
                exception.Errors,
                error => error.Field == nameof(OficinaDTO.Cep) && error.Message == "CEP deve ser maior que zero.");
            _repository.Verify(repository => repository.Add(It.IsAny<Oficina>()), Times.Never);
        }

        [Theory]
        [InlineData("")]
        [InlineData("curta1")]
        [InlineData("senhasemnumero")]
        [InlineData("12345678")]
        public async Task Create_DeveRejeitarSenhaForaDaPolitica(string password)
        {
            var request = new OficinaRequestDTO
            {
                CNPJ = "11222333000181",
                Email = "oficina@example.com",
                Senha = password,
                Cep = 89010000
            };
            _cnpjValidator.Setup(validator => validator.IsValid(request.CNPJ)).Returns(true);
            _repository
                .Setup(repository => repository.ExistsByEmail("oficina@example.com", null))
                .ReturnsAsync(false);

            var exception = await Assert.ThrowsAsync<BusinessValidationException>(
                () => CreateService().Create(request));

            Assert.Contains(
                exception.Errors,
                error => error.Field == nameof(OficinaRequestDTO.Senha));
            _repository.Verify(repository => repository.Add(It.IsAny<Oficina>()), Times.Never);
        }

        [Fact]
        public async Task UpdateSelfProfile_DeveRejeitarCepZero()
        {
            var request = new OficinaRequestDTO { Cep = 0 };

            var exception = await Assert.ThrowsAsync<BusinessValidationException>(
                () => CreateService().UpdateSelfProfile(request, 7));

            Assert.Contains(
                exception.Errors,
                error => error.Field == nameof(OficinaDTO.Cep) && error.Message == "CEP deve ser maior que zero.");
            _repository.Verify(repository => repository.SaveChanges(), Times.Never);
        }

        [Fact]
        public async Task DeactivateAsync_DeveExecutarInativacaoLogica()
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            _repository
                .Setup(repository => repository.DeactivateAsync(7, cancellationTokenSource.Token))
                .ReturnsAsync(true);
            var service = CreateService();

            await service.DeactivateAsync(7, cancellationTokenSource.Token);

            _repository.Verify(
                repository => repository.DeactivateAsync(7, cancellationTokenSource.Token),
                Times.Once);
            _repository.Verify(repository => repository.Remove(It.IsAny<Oficina>()), Times.Never);
        }

        [Fact]
        public async Task DeactivateAsync_DeveFalharQuandoOficinaNaoExiste()
        {
            _repository
                .Setup(repository => repository.DeactivateAsync(99, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            var service = CreateService();

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => service.DeactivateAsync(99));
        }

        private OficinaService CreateService() =>
            new(
                _repository.Object,
                _mapper.Object,
                _cnpjValidator.Object,
                _passwordHasher.Object);
    }
}
