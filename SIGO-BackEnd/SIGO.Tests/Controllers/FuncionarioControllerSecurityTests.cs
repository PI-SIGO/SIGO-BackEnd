using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SIGO.Controllers;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Security;
using SIGO.Services.Interfaces;
using Xunit;

namespace SIGO.Tests.Controllers
{
    public class FuncionarioControllerSecurityTests
    {
        private readonly Mock<IFuncionarioService> _funcionarioServiceMock = new();
        private readonly Mock<IMapper> _mapperMock = new();
        private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
        private readonly Mock<IJwtTokenService> _jwtTokenServiceMock = new();
        private readonly Mock<IFuncionarioRoleResolver> _roleResolverMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();

        [Fact]
        public async Task Post_DeveForcarFuncionarioEOficinaDoJwt_QuandoOficinaCriaFuncionario()
        {
            FuncionarioRequestDTO? recebido = null;
            _currentUserServiceMock.Setup(s => s.IsInRole(SystemRoles.Admin)).Returns(false);
            _currentUserServiceMock.Setup(s => s.OficinaId).Returns(4);
            _funcionarioServiceMock
                .Setup(s => s.Create(It.IsAny<FuncionarioRequestDTO>()))
                .Callback<FuncionarioRequestDTO>(dto => recebido = dto)
                .Returns(Task.CompletedTask);
            var controller = CreateController();
            var dto = CriarFuncionarioRequest();
            dto.Role = SystemRoles.Admin;
            dto.IdOficina = 999;

            var result = await controller.Post(dto);

            Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(recebido);
            Assert.Equal(SystemRoles.Funcionario, recebido!.Role);
            Assert.Equal(4, recebido.IdOficina);
        }

        [Fact]
        public async Task Login_NaoDeveDerivarAdminAPartirDoCargo()
        {
            _funcionarioServiceMock
                .Setup(s => s.Login(It.IsAny<Login>()))
                .ReturnsAsync(new FuncionarioDTO
                {
                    Id = 10,
                    Nome = "Funcionario",
                    Email = "func@test.com",
                    Cargo = "ADMIN",
                    Role = SystemRoles.Funcionario,
                    IdOficina = 2
                });
            _roleResolverMock.Setup(r => r.Resolve(SystemRoles.Funcionario)).Returns(SystemRoles.Funcionario);
            _jwtTokenServiceMock.Setup(s => s.GenerateToken(It.IsAny<JwtTokenRequest>())).Returns("token");
            var controller = CreateController();

            var result = await controller.Login(new Login { Email = "func@test.com", Password = "senha" });

            Assert.IsType<OkObjectResult>(result);
            _jwtTokenServiceMock.Verify(s => s.GenerateToken(It.Is<JwtTokenRequest>(r =>
                r.Role == SystemRoles.Funcionario &&
                r.OficinaId == 2)), Times.Once);
            _jwtTokenServiceMock.Verify(s => s.GenerateToken(It.Is<JwtTokenRequest>(r => r.Role == SystemRoles.Admin)), Times.Never);
        }

        [Fact]
        public async Task Post_NaoDeveSerializarSenha_NaResposta()
        {
            _currentUserServiceMock.Setup(s => s.IsInRole(SystemRoles.Admin)).Returns(false);
            _currentUserServiceMock.Setup(s => s.OficinaId).Returns(4);
            _funcionarioServiceMock.Setup(s => s.Create(It.IsAny<FuncionarioRequestDTO>())).Returns(Task.CompletedTask);
            var controller = CreateController();

            var result = await controller.Post(CriarFuncionarioRequest());

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<Response>(ok.Value);
            Assert.IsType<FuncionarioDTO>(response.Data);
        }

        private FuncionarioController CreateController()
        {
            return new FuncionarioController(
                _funcionarioServiceMock.Object,
                _mapperMock.Object,
                _passwordHasherMock.Object,
                _jwtTokenServiceMock.Object,
                _roleResolverMock.Object,
                _currentUserServiceMock.Object);
        }

        private static FuncionarioRequestDTO CriarFuncionarioRequest()
        {
            return new FuncionarioRequestDTO
            {
                Nome = "Funcionario",
                Cpf = "52998224725",
                Cargo = "Mecanico",
                Email = "func@test.com",
                Senha = "senha",
                IdOficina = 1,
                Role = SystemRoles.Funcionario
            };
        }
    }
}
