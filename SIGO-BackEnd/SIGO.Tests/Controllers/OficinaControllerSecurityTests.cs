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
    public class OficinaControllerSecurityTests
    {
        private readonly Mock<IOficinaService> _oficinaServiceMock = new();
        private readonly Mock<IMapper> _mapperMock = new();
        private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
        private readonly Mock<IJwtTokenService> _jwtTokenServiceMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();

        [Fact]
        public async Task Update_DeveRetornarForbid_QuandoOficinaTentaAtualizarOutraOficina()
        {
            _currentUserServiceMock.Setup(s => s.IsInRole(SystemRoles.Oficina)).Returns(true);
            _currentUserServiceMock.Setup(s => s.OficinaId).Returns(1);
            var controller = CreateController();

            var result = await controller.Update(2, CriarOficinaRequest());

            Assert.IsType<ForbidResult>(result);
            _oficinaServiceMock.Verify(s => s.UpdateSelfProfile(It.IsAny<OficinaRequestDTO>(), It.IsAny<int>()), Times.Never);
            _oficinaServiceMock.Verify(s => s.Update(It.IsAny<OficinaRequestDTO>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Update_DeveForcarOficinaDoJwt_QuandoOficinaAtualizaProprioPerfil()
        {
            _currentUserServiceMock.Setup(s => s.IsInRole(SystemRoles.Oficina)).Returns(true);
            _currentUserServiceMock.Setup(s => s.OficinaId).Returns(7);
            _oficinaServiceMock.Setup(s => s.UpdateSelfProfile(It.IsAny<OficinaRequestDTO>(), 7)).Returns(Task.CompletedTask);
            _oficinaServiceMock.Setup(s => s.GetById(7)).ReturnsAsync(new OficinaDTO { Id = 7, Nome = "Oficina" });
            var controller = CreateController();

            var result = await controller.Update(7, CriarOficinaRequest());

            Assert.IsType<OkObjectResult>(result);
            _oficinaServiceMock.Verify(s => s.UpdateSelfProfile(It.Is<OficinaRequestDTO>(o => o.Id == 7), 7), Times.Once);
            _oficinaServiceMock.Verify(s => s.Update(It.IsAny<OficinaRequestDTO>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void Controller_DeveExporSomenteRotaV1()
        {
            var routes = typeof(OficinaController)
                .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
                .Cast<RouteAttribute>()
                .Select(attribute => attribute.Template)
                .ToArray();

            Assert.Equal("api/v1/oficinas", Assert.Single(routes));
        }

        [Fact]
        public void Update_NaoDeveAutorizarFuncionario()
        {
            var attribute = typeof(OficinaController)
                .GetMethod(nameof(OficinaController.Update))!
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
                .Cast<AuthorizeAttribute>()
                .Single();
            var roles = attribute.Roles ?? string.Empty;

            Assert.Contains(SystemRoles.Admin, roles);
            Assert.Contains(SystemRoles.Oficina, roles);
            Assert.DoesNotContain(SystemRoles.Funcionario, roles);
        }

        [Fact]
        public async Task Delete_DeveRetornarForbid_QuandoOficinaTentaInativarOutraOficina()
        {
            _currentUserServiceMock.Setup(s => s.IsInRole(SystemRoles.Admin)).Returns(false);
            _currentUserServiceMock.Setup(s => s.IsInRole(SystemRoles.Oficina)).Returns(true);
            _currentUserServiceMock.Setup(s => s.OficinaId).Returns(7);
            var controller = CreateController();

            var result = await controller.Delete(8);

            Assert.IsType<ForbidResult>(result);
            _oficinaServiceMock.Verify(
                service => service.DeactivateAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _oficinaServiceMock.Verify(service => service.Remove(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Delete_DeveInativarPropriaOficinaERetornarNoContent()
        {
            _currentUserServiceMock.Setup(s => s.IsInRole(SystemRoles.Admin)).Returns(false);
            _currentUserServiceMock.Setup(s => s.IsInRole(SystemRoles.Oficina)).Returns(true);
            _currentUserServiceMock.Setup(s => s.OficinaId).Returns(7);
            _oficinaServiceMock
                .Setup(service => service.DeactivateAsync(7, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var controller = CreateController();

            var result = await controller.Delete(7);

            Assert.IsType<NoContentResult>(result);
            _oficinaServiceMock.Verify(
                service => service.DeactivateAsync(7, It.IsAny<CancellationToken>()),
                Times.Once);
            _oficinaServiceMock.Verify(service => service.Remove(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Create_DeveRetornarCreated()
        {
            _oficinaServiceMock
                .Setup(service => service.Create(It.IsAny<OficinaRequestDTO>()))
                .Callback<OficinaRequestDTO>(request => request.Id = 31)
                .Returns(Task.CompletedTask);
            _oficinaServiceMock
                .Setup(service => service.GetById(31))
                .ReturnsAsync(new OficinaDTO { Id = 31, Nome = "Oficina" });
            var controller = CreateController();

            var result = await controller.Create(CriarOficinaRequest());

            var created = Assert.IsType<CreatedResult>(result);
            Assert.Equal("/api/v1/oficinas/31", created.Location);
            Assert.IsType<OficinaDTO>(created.Value);
        }

        [Fact]
        public async Task Login_DeveRetornarUnauthorized_QuandoCredenciaisSaoInvalidas()
        {
            _oficinaServiceMock
                .Setup(service => service.Login(It.IsAny<SIGO.Objects.Contracts.Login>()))
                .ReturnsAsync((OficinaDTO?)null);
            var controller = CreateController();

            var result = await controller.Login(new SIGO.Objects.Contracts.Login
            {
                Email = "oficina@test.com",
                Password = "invalida"
            });

            var unauthorized = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
            Assert.IsType<ProblemDetails>(unauthorized.Value);
        }

        [Fact]
        public async Task Login_DeveRetornarContratoBearerPadronizado()
        {
            _oficinaServiceMock
                .Setup(service => service.Login(It.IsAny<Login>()))
                .ReturnsAsync(new OficinaDTO
                {
                    Id = 7,
                    Nome = "Oficina",
                    Email = "oficina@test.com"
                });
            _jwtTokenServiceMock
                .Setup(service => service.GenerateToken(It.IsAny<JwtTokenRequest>()))
                .Returns("jwt-oficina");
            var controller = CreateController();

            var result = await controller.Login(new Login
            {
                Email = "oficina@test.com",
                Password = "Senha123"
            });

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<AccessTokenResponse>(ok.Value);
            Assert.Equal("jwt-oficina", response.AccessToken);
            Assert.Equal("Bearer", response.TokenType);
        }

        [Fact]
        public async Task Get_DeveRetornarPagedResponseDireto()
        {
            _oficinaServiceMock
                .Setup(service => service.GetAll())
                .ReturnsAsync(new[]
                {
                    new OficinaDTO { Id = 1, Nome = "Oficina 1" },
                    new OficinaDTO { Id = 2, Nome = "Oficina 2" },
                    new OficinaDTO { Id = 3, Nome = "Oficina 3" }
                });
            var controller = CreateController();

            var result = await controller.Get(new PaginationRequest
            {
                Page = 2,
                PageSize = 1
            });

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var page = Assert.IsType<PagedResponse<OficinaDTO>>(ok.Value);
            Assert.Equal(3, page.TotalItems);
            Assert.Equal(3, page.TotalPages);
            Assert.Equal(2, page.Items.Single().Id);
        }

        private OficinaController CreateController()
        {
            return new OficinaController(
                _oficinaServiceMock.Object,
                _mapperMock.Object,
                _passwordHasherMock.Object,
                _jwtTokenServiceMock.Object,
                _currentUserServiceMock.Object);
        }

        private static OficinaRequestDTO CriarOficinaRequest()
        {
            return new OficinaRequestDTO
            {
                Nome = "Oficina",
                CNPJ = "11222333000181",
                Email = "oficina@test.com",
                Senha = "senha",
                Rua = "Rua A",
                Cidade = "Cidade",
                Bairro = "Centro",
                Estado = "SP",
                Pais = "Brasil"
            };
        }
    }
}
