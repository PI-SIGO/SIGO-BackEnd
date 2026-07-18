using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SIGO.Controllers;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Enums;
using SIGO.Services.Interfaces;
using Xunit;

namespace SIGO.Tests.Controllers
{
    public sealed class ClienteRegistrationControllerTests
    {
        [Fact]
        public void Register_DeveExporPostAnonimoNoEndpointDeCadastros()
        {
            var controllerRoute = typeof(ClienteRegistrationController)
                .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
                .Cast<RouteAttribute>()
                .Select(attribute => attribute.Template)
                .ToArray();
            var method = typeof(ClienteRegistrationController)
                .GetMethod(nameof(ClienteRegistrationController.Register));

            Assert.NotNull(method);
            Assert.Equal("api/v1/clientes/cadastros", Assert.Single(controllerRoute));
            Assert.Single(method!.GetCustomAttributes(typeof(HttpPostAttribute), inherit: false));
            Assert.Single(method.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: false));
        }

        [Fact]
        public async Task Register_DeveCadastrarDiretamenteERetornarCreated()
        {
            var request = new CadastrarClienteDTO
            {
                Cpf = "529.982.247-25",
                Nome = "Cliente Teste",
                Email = "Cliente@Example.com",
                Senha = "Senha123"
            };
            var response = new CadastroClienteResultadoDTO(
                42,
                "Cliente Teste",
                "cliente@example.com");
            var service = new Mock<IClienteRegistrationService>();
            service
                .Setup(item => item.RegisterAsync(
                    request,
                    It.IsAny<SecurityAuditContext>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            using var cancellationTokenSource = new CancellationTokenSource();
            var httpContext = new DefaultHttpContext
            {
                TraceIdentifier = "cadastro-request"
            };
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            var controller = new ClienteRegistrationController(service.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };

            var result = await controller.Register(request, cancellationTokenSource.Token);

            var created = Assert.IsType<CreatedResult>(result);
            Assert.Equal("/api/v1/clientes/42", created.Location);
            Assert.Same(response, created.Value);
            service.Verify(item => item.RegisterAsync(
                request,
                It.Is<SecurityAuditContext>(context =>
                    context.TipoAtor == TipoAtorAuditoria.Anonimo &&
                    context.AtorId == null &&
                    context.IpAddress == "127.0.0.1" &&
                    context.CorrelationId == "cadastro-request"),
                cancellationTokenSource.Token),
                Times.Once);
        }
    }
}
