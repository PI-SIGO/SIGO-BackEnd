using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
            _currentUserServiceMock.Setup(s => s.IsInRole(SystemRoles.Oficina)).Returns(true);
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

            var created = Assert.IsType<CreatedResult>(result);
            Assert.Equal(201, created.StatusCode);
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

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<AccessTokenResponse>(ok.Value);
            Assert.Equal("token", response.AccessToken);
            Assert.Equal("Bearer", response.TokenType);
            _jwtTokenServiceMock.Verify(s => s.GenerateToken(It.Is<JwtTokenRequest>(r =>
                r.Role == SystemRoles.Funcionario &&
                r.OficinaId == 2)), Times.Once);
            _jwtTokenServiceMock.Verify(s => s.GenerateToken(It.Is<JwtTokenRequest>(r => r.Role == SystemRoles.Admin)), Times.Never);
        }

        [Fact]
        public async Task Post_NaoDeveSerializarSenha_NaResposta()
        {
            _currentUserServiceMock.Setup(s => s.IsInRole(SystemRoles.Admin)).Returns(false);
            _currentUserServiceMock.Setup(s => s.IsInRole(SystemRoles.Oficina)).Returns(true);
            _currentUserServiceMock.Setup(s => s.OficinaId).Returns(4);
            _funcionarioServiceMock.Setup(s => s.Create(It.IsAny<FuncionarioRequestDTO>())).Returns(Task.CompletedTask);
            var controller = CreateController();

            var result = await controller.Post(CriarFuncionarioRequest());

            var created = Assert.IsType<CreatedResult>(result);
            Assert.Equal(201, created.StatusCode);
            Assert.IsType<FuncionarioDTO>(created.Value);
        }

        [Fact]
        public void Controller_DeveExporSomenteRotaV1()
        {
            var routes = typeof(FuncionarioController)
                .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
                .Cast<RouteAttribute>()
                .Select(attribute => attribute.Template)
                .ToArray();

            Assert.Equal("api/v1/funcionarios", Assert.Single(routes));
        }

        [Theory]
        [InlineData(nameof(FuncionarioController.Post))]
        [InlineData(nameof(FuncionarioController.Put))]
        [InlineData(nameof(FuncionarioController.DeleteFuncionario))]
        public void Escritas_NaoDevemAutorizarFuncionario(string methodName)
        {
            var attribute = typeof(FuncionarioController)
                .GetMethod(methodName)!
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
                .Cast<AuthorizeAttribute>()
                .Single();
            var roles = attribute.Roles ?? string.Empty;

            Assert.Contains(SystemRoles.Admin, roles);
            Assert.Contains(SystemRoles.Oficina, roles);
            Assert.DoesNotContain(SystemRoles.Funcionario, roles);
        }

        [Fact]
        public async Task Post_DeveRetornarForbid_QuandoChamadoPorFuncionario()
        {
            _currentUserServiceMock.Setup(s => s.IsInRole(SystemRoles.Admin)).Returns(false);
            _currentUserServiceMock.Setup(s => s.IsInRole(SystemRoles.Oficina)).Returns(false);
            var controller = CreateController();

            var result = await controller.Post(CriarFuncionarioRequest());

            Assert.IsType<ForbidResult>(result);
            _funcionarioServiceMock.Verify(
                service => service.Create(It.IsAny<FuncionarioRequestDTO>()),
                Times.Never);
        }

        [Fact]
        public async Task Put_DeveRetornarIdDaRota()
        {
            _currentUserServiceMock.Setup(s => s.IsInRole(SystemRoles.Admin)).Returns(true);
            _funcionarioServiceMock
                .Setup(service => service.GetById(42))
                .ReturnsAsync(new FuncionarioDTO { Id = 42 });
            _funcionarioServiceMock
                .Setup(service => service.Update(It.IsAny<FuncionarioRequestDTO>(), 42))
                .Returns(Task.CompletedTask);
            var controller = CreateController();
            var request = CriarFuncionarioRequest();
            request.Id = 999;

            var result = await controller.Put(42, request);

            var ok = Assert.IsType<OkObjectResult>(result);
            var funcionario = Assert.IsType<FuncionarioDTO>(ok.Value);
            Assert.Equal(42, funcionario.Id);
            _funcionarioServiceMock.Verify(
                service => service.Update(
                    It.Is<FuncionarioRequestDTO>(dto => dto.Id == 42),
                    42),
                Times.Once);
        }

        [Fact]
        public async Task Delete_DeveInativarFuncionarioDaPropriaOficinaERetornarNoContent()
        {
            _currentUserServiceMock.Setup(s => s.IsInRole(SystemRoles.Admin)).Returns(false);
            _currentUserServiceMock.Setup(s => s.IsInRole(SystemRoles.Oficina)).Returns(true);
            _currentUserServiceMock.Setup(s => s.OficinaId).Returns(4);
            _funcionarioServiceMock
                .Setup(service => service.GetByIdForOficina(8, 4))
                .ReturnsAsync(new FuncionarioDTO { Id = 8, IdOficina = 4 });
            _funcionarioServiceMock
                .Setup(service => service.DeactivateAsync(8, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var controller = CreateController();

            var result = await controller.DeleteFuncionario(8);

            Assert.IsType<NoContentResult>(result);
            _funcionarioServiceMock.Verify(
                service => service.DeactivateAsync(8, It.IsAny<CancellationToken>()),
                Times.Once);
            _funcionarioServiceMock.Verify(service => service.Remove(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Login_DeveRetornarUnauthorized_QuandoCredenciaisSaoInvalidas()
        {
            _funcionarioServiceMock
                .Setup(service => service.Login(It.IsAny<Login>()))
                .ReturnsAsync((FuncionarioDTO?)null);
            var controller = CreateController();

            var result = await controller.Login(new Login
            {
                Email = "func@test.com",
                Password = "invalida"
            });

            var unauthorized = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
            Assert.IsType<ProblemDetails>(unauthorized.Value);
        }

        [Fact]
        public async Task GetAll_DeveRetornarPagedResponseDireto()
        {
            _currentUserServiceMock.Setup(service => service.IsInRole(SystemRoles.Admin)).Returns(true);
            _funcionarioServiceMock
                .Setup(service => service.GetAll())
                .ReturnsAsync(new[]
                {
                    new FuncionarioDTO { Id = 1, Nome = "Funcionário 1" },
                    new FuncionarioDTO { Id = 2, Nome = "Funcionário 2" },
                    new FuncionarioDTO { Id = 3, Nome = "Funcionário 3" }
                });
            var controller = CreateController();

            var result = await controller.GetAll(new PaginationRequest
            {
                Page = 2,
                PageSize = 1
            });

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var page = Assert.IsType<PagedResponse<FuncionarioDTO>>(ok.Value);
            Assert.Equal(3, page.TotalItems);
            Assert.Equal(3, page.TotalPages);
            Assert.Equal(2, page.Items.Single().Id);
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
