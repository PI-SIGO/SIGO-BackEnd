using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using SIGO.Controllers;
using SIGO.Objects.Dtos.Entities;
using SIGO.Security;
using SIGO.Services.Interfaces;
using Xunit;

namespace SIGO.Tests.Controllers
{
    public class ClienteControllerTests
    {
        private readonly Mock<IClienteService> _clienteServiceMock = new();
        private readonly Mock<IMapper> _mapperMock = new();
        private readonly IConfiguration _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "SIGO_Barrament_API_Autentication",
                ["Jwt:Issuer"] = "SIGO API",
                ["Jwt:Audience"] = "SIGO Website"
            })
            .Build();

        [Fact]
        public async Task GetByIdWithDetails_DeveRetornarForbid_QuandoClienteTentaVerOutroCadastro()
        {
            var controller = CreateController(userId: 1, SystemRoles.Cliente);

            var result = await controller.GetByIdWithDetails(2);

            Assert.IsType<ForbidResult>(result);
            _clienteServiceMock.Verify(s => s.GetByIdWithDetails(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Delete_DeveRetornarForbid_QuandoClienteTentaExcluirOutroCadastro()
        {
            var controller = CreateController(userId: 1, SystemRoles.Cliente);

            var result = await controller.Delete(2);

            Assert.IsType<ForbidResult>(result);
            _clienteServiceMock.Verify(s => s.Remove(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Put_DevePermitir_QuandoClienteEditaProprioCadastro()
        {
            var dto = CriarClienteDto(id: 1);
            _clienteServiceMock.Setup(s => s.GetById(1)).ReturnsAsync(dto);
            _clienteServiceMock.Setup(s => s.ValidarCpfCnpj(dto.Cpf_Cnpj, 1)).Returns(Task.CompletedTask);
            _clienteServiceMock.Setup(s => s.Update(It.IsAny<ClienteDTO>(), 1)).Returns(Task.CompletedTask);

            var controller = CreateController(userId: 1, SystemRoles.Cliente);

            var result = await controller.Put(1, dto);

            Assert.IsType<OkObjectResult>(result);
            _clienteServiceMock.Verify(s => s.Update(It.IsAny<ClienteDTO>(), 1), Times.Once);
        }

        private ClienteController CreateController(int? userId = null, params string[] roles)
        {
            var controller = new ClienteController(
                _clienteServiceMock.Object,
                _mapperMock.Object,
                _configuration);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = CreatePrincipal(userId, roles)
                }
            };

            return controller;
        }

        private static ClaimsPrincipal CreatePrincipal(int? userId, params string[] roles)
        {
            var claims = new List<Claim>();

            if (userId.HasValue)
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var identity = new ClaimsIdentity(claims, authenticationType: "Test");
            return new ClaimsPrincipal(identity);
        }

        private static ClienteDTO CriarClienteDto(int id)
        {
            return new ClienteDTO
            {
                Id = id,
                Nome = "Cliente",
                Email = "cliente@test.com",
                senha = "123",
                Cpf_Cnpj = "12345678901",
                Obs = string.Empty,
                razao = string.Empty,
                Rua = "Rua A",
                Cidade = "Cidade",
                Cep = "12345678",
                Bairro = "Centro",
                Estado = "SP",
                Pais = "Brasil",
                Complemento = string.Empty
            };
        }
    }
}
