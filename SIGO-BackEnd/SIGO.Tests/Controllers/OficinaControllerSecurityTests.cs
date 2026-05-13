using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SIGO.Controllers;
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
            var controller = CreateController();

            var result = await controller.Update(7, CriarOficinaRequest());

            Assert.IsType<OkObjectResult>(result);
            _oficinaServiceMock.Verify(s => s.UpdateSelfProfile(It.Is<OficinaRequestDTO>(o => o.Id == 7), 7), Times.Once);
            _oficinaServiceMock.Verify(s => s.Update(It.IsAny<OficinaRequestDTO>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void Delete_DeveSerRestritoAoAdmin()
        {
            var attribute = typeof(OficinaController)
                .GetMethod(nameof(OficinaController.Delete))!
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
                .Cast<AuthorizeAttribute>()
                .Single();

            Assert.Equal(SystemRoles.Admin, attribute.Roles);
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
