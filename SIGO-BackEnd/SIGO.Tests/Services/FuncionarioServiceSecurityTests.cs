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
    public sealed class FuncionarioServiceSecurityTests
    {
        private readonly Mock<IFuncionarioRepository> _repository = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ICpfValidator> _cpfValidator = new();
        private readonly Mock<IPasswordHasher> _passwordHasher = new();

        [Fact]
        public async Task Login_DeveNegarFuncionarioInativo_MesmoComSenhaCorreta()
        {
            var funcionario = new Funcionario
            {
                Id = 8,
                Email = "func@test.com",
                Senha = "hash",
                Situacao = Situacao.INATIVO
            };
            _repository
                .Setup(repository => repository.GetByEmail("func@test.com"))
                .ReturnsAsync(funcionario);
            _passwordHasher
                .Setup(hasher => hasher.Verify("Senha123", "hash"))
                .Returns(true);
            var service = CreateService();

            var result = await service.Login(new Login
            {
                Email = "func@test.com",
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
            var funcionario = new Funcionario
            {
                Id = 1,
                Email = "admin@test.com",
                Senha = "hash",
                Role = SystemRoles.Admin,
                Situacao = Situacao.ATIVO
            };
            var expected = new FuncionarioDTO { Id = 1, Email = "admin@test.com" };
            _repository
                .Setup(repository => repository.GetByEmail("admin@test.com"))
                .ReturnsAsync(funcionario);
            _passwordHasher
                .Setup(hasher => hasher.Verify("Senha123", "hash"))
                .Returns(true);
            _mapper
                .Setup(mapper => mapper.Map<FuncionarioDTO?>(funcionario))
                .Returns(expected);

            var result = await CreateService().Login(new Login
            {
                Email = "  ADMIN@TEST.COM  ",
                Password = "Senha123"
            });

            Assert.Same(expected, result);
            _repository.Verify(
                repository => repository.GetByEmail("admin@test.com"),
                Times.Once);
        }

        [Fact]
        public async Task Create_DevePersistirEmailNormalizado()
        {
            var request = new FuncionarioRequestDTO
            {
                Cpf = "52998224725",
                Email = "  FUNCIONARIO@EXAMPLE.COM  ",
                Senha = "Senha123",
                IdOficina = 4,
                Role = SystemRoles.Funcionario
            };
            var entity = new Funcionario { Id = 12 };
            _cpfValidator.Setup(validator => validator.IsValid(request.Cpf)).Returns(true);
            _cpfValidator.Setup(validator => validator.Normalize(request.Cpf)).Returns("52998224725");
            _repository
                .Setup(repository => repository.ExistsByEmail("funcionario@example.com", null))
                .ReturnsAsync(false);
            _mapper.Setup(mapper => mapper.Map<Funcionario>(request)).Returns(entity);
            _passwordHasher.Setup(hasher => hasher.Hash("Senha123")).Returns("hash");

            await CreateService().Create(request);

            Assert.Equal("funcionario@example.com", request.Email);
            _repository.Verify(repository => repository.Add(entity), Times.Once);
        }

        [Fact]
        public async Task Create_DeveRejeitarEmailDuplicadoIgnorandoCaseEEspacos()
        {
            var request = new FuncionarioRequestDTO
            {
                Cpf = "52998224725",
                Email = "  FUNCIONARIO@EXAMPLE.COM  ",
                Senha = "Senha123",
                IdOficina = 4,
                Role = SystemRoles.Funcionario
            };
            _cpfValidator.Setup(validator => validator.IsValid(request.Cpf)).Returns(true);
            _cpfValidator.Setup(validator => validator.Normalize(request.Cpf)).Returns("52998224725");
            _repository
                .Setup(repository => repository.ExistsByEmail("funcionario@example.com", null))
                .ReturnsAsync(true);

            var exception = await Assert.ThrowsAsync<BusinessValidationException>(
                () => CreateService().Create(request));

            Assert.Contains(
                exception.Errors,
                error => error.Field == nameof(FuncionarioDTO.Email) && error.Message == "E-mail já cadastrado.");
            _repository.Verify(repository => repository.Add(It.IsAny<Funcionario>()), Times.Never);
        }

        [Fact]
        public async Task Login_DeveNegarFuncionarioQuandoOficinaEstaInativa()
        {
            var funcionario = new Funcionario
            {
                Id = 8,
                Email = "func@test.com",
                Senha = "hash",
                Role = SystemRoles.Funcionario,
                Situacao = Situacao.ATIVO,
                IdOficina = 4,
                Oficina = new Oficina
                {
                    Id = 4,
                    Situacao = Situacao.INATIVO
                }
            };
            _repository
                .Setup(repository => repository.GetByEmail("func@test.com"))
                .ReturnsAsync(funcionario);
            _passwordHasher
                .Setup(hasher => hasher.Verify("Senha123", "hash"))
                .Returns(true);
            var service = CreateService();

            var result = await service.Login(new Login
            {
                Email = "func@test.com",
                Password = "Senha123"
            });

            Assert.Null(result);
            _repository.Verify(
                repository => repository.UpdatePasswordHash(It.IsAny<int>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Login_DevePermitirAdminAtivoSemOficina()
        {
            var funcionario = new Funcionario
            {
                Id = 1,
                Nome = "Administrador",
                Email = "admin@test.com",
                Senha = "hash",
                Role = SystemRoles.Admin,
                Situacao = Situacao.ATIVO,
                IdOficina = null
            };
            var expected = new SIGO.Objects.Dtos.Entities.FuncionarioDTO
            {
                Id = 1,
                Nome = "Administrador",
                Email = "admin@test.com",
                Role = SystemRoles.Admin
            };
            _repository
                .Setup(repository => repository.GetByEmail("admin@test.com"))
                .ReturnsAsync(funcionario);
            _passwordHasher
                .Setup(hasher => hasher.Verify("Senha123", "hash"))
                .Returns(true);
            _mapper
                .Setup(mapper => mapper.Map<SIGO.Objects.Dtos.Entities.FuncionarioDTO?>(funcionario))
                .Returns(expected);
            var service = CreateService();

            var result = await service.Login(new Login
            {
                Email = "admin@test.com",
                Password = "Senha123"
            });

            Assert.Same(expected, result);
        }

        [Fact]
        public async Task DeactivateAsync_DeveExecutarInativacaoLogica()
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            _repository
                .Setup(repository => repository.DeactivateAsync(8, cancellationTokenSource.Token))
                .ReturnsAsync(true);
            var service = CreateService();

            await service.DeactivateAsync(8, cancellationTokenSource.Token);

            _repository.Verify(
                repository => repository.DeactivateAsync(8, cancellationTokenSource.Token),
                Times.Once);
            _repository.Verify(repository => repository.Remove(It.IsAny<Funcionario>()), Times.Never);
        }

        [Fact]
        public async Task DeactivateAsync_DeveFalharQuandoFuncionarioNaoExiste()
        {
            _repository
                .Setup(repository => repository.DeactivateAsync(99, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            var service = CreateService();

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => service.DeactivateAsync(99));
        }

        private FuncionarioService CreateService() =>
            new(
                _repository.Object,
                _mapper.Object,
                _cpfValidator.Object,
                _passwordHasher.Object);
    }
}
