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
    public class OpcoesCadastroControllerTests
    {
        [Fact]
        public void Controller_DeveAutorizarSomenteOficinaEFuncionario()
        {
            var attribute = typeof(OpcoesCadastroController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
                .Cast<AuthorizeAttribute>()
                .Single();
            var roles = (attribute.Roles ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(2, roles.Length);
            Assert.Contains(SystemRoles.Oficina, roles);
            Assert.Contains(SystemRoles.Funcionario, roles);
            Assert.DoesNotContain(SystemRoles.Admin, roles);
            Assert.DoesNotContain(SystemRoles.Cliente, roles);
        }

        [Fact]
        public async Task Get_DeveUsarOficinaDoJwt()
        {
            var service = new Mock<IOpcoesCadastroService>();
            var currentUser = new Mock<ICurrentUserService>();
            var expected = EmptyResponse();
            currentUser.Setup(user => user.OficinaId).Returns(7);
            service
                .Setup(item => item.GetByOficinaAsync(7, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);
            var controller = new OpcoesCadastroController(service.Object, currentUser.Object);

            var result = await controller.Get();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(expected, ok.Value);
            service.Verify(
                item => item.GetByOficinaAsync(7, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Get_DeveRetornarForbid_QuandoTokenNaoTemOficina()
        {
            var service = new Mock<IOpcoesCadastroService>();
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.Setup(user => user.OficinaId).Returns((int?)null);
            var controller = new OpcoesCadastroController(service.Object, currentUser.Object);

            var result = await controller.Get();

            Assert.IsType<ForbidResult>(result.Result);
            service.Verify(
                item => item.GetByOficinaAsync(
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static OpcoesCadastroDTO EmptyResponse() => new(
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>());
    }
}
