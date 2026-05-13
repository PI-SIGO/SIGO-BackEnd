using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SIGO.Controllers;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Security;
using SIGO.Services.Interfaces;
using System.Text.Json;
using Xunit;

namespace SIGO.Tests.Controllers
{
    public class ClienteControllerTests
    {
        private readonly Mock<IClienteService> _clienteServiceMock = new();
        private readonly Mock<IMapper> _mapperMock = new();
        private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
        private readonly Mock<IJwtTokenService> _jwtTokenServiceMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();

        [Fact]
        public async Task GetByIdWithDetails_DeveRetornarForbid_QuandoClienteTentaVerOutroCadastro()
        {
            var controller = CreateController(userId: 1, roles: new[] { SystemRoles.Cliente });

            var result = await controller.GetByIdWithDetails(2);

            Assert.IsType<ForbidResult>(result);
            _clienteServiceMock.Verify(s => s.GetByIdWithDetails(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Delete_DeveRetornarForbid_QuandoClienteTentaExcluirOutroCadastro()
        {
            var controller = CreateController(userId: 1, roles: new[] { SystemRoles.Cliente });

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
            _clienteServiceMock.Setup(s => s.Update(It.IsAny<ClienteRequestDTO>(), 1)).Returns(Task.CompletedTask);

            var controller = CreateController(userId: 1, roles: new[] { SystemRoles.Cliente });

            var result = await controller.Put(1, dto);

            Assert.IsType<OkObjectResult>(result);
            _clienteServiceMock.Verify(s => s.Update(It.IsAny<ClienteRequestDTO>(), 1), Times.Once);
        }

        [Fact]
        public async Task Post_DeveUsarOficinaIdDoJwt_QuandoOficinaRegistraCliente()
        {
            var dto = CriarClienteDto();
            var vinculado = CriarClienteDto(id: 10);

            _clienteServiceMock
                .Setup(s => s.CreateForOficina(It.IsAny<ClienteRequestDTO>(), 7))
                .ReturnsAsync(vinculado);

            var controller = CreateController(userId: 99, roles: new[] { SystemRoles.Oficina }, oficinaId: 7);

            var result = await controller.Post(dto);

            Assert.IsType<OkObjectResult>(result);
            _clienteServiceMock.Verify(s => s.CreateForOficina(
                It.Is<ClienteRequestDTO>(c => c.senha == "123"),
                7),
                Times.Once);
            _clienteServiceMock.Verify(s => s.Create(It.IsAny<ClienteRequestDTO>()), Times.Never);
        }

        [Fact]
        public async Task Post_DeveFazerCadastroSelfService_QuandoNaoHaUsuarioDeOficina()
        {
            var dto = CriarClienteDto();
            string? senhaEnviadaAoService = null;
            _clienteServiceMock
                .Setup(s => s.Create(It.IsAny<ClienteRequestDTO>()))
                .Callback<ClienteRequestDTO>(c => senhaEnviadaAoService = c.senha)
                .Returns(Task.CompletedTask);

            var controller = CreateController();

            var result = await controller.Post(dto);

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal("123", senhaEnviadaAoService);
            _clienteServiceMock.Verify(s => s.Create(It.IsAny<ClienteRequestDTO>()), Times.Once);
            _clienteServiceMock.Verify(s => s.CreateForOficina(It.IsAny<ClienteRequestDTO>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Post_NaoDeveSerializarSenha_EmResposta()
        {
            var dto = CriarClienteDto();
            _clienteServiceMock.Setup(s => s.Create(It.IsAny<ClienteRequestDTO>())).Returns(Task.CompletedTask);

            var controller = CreateController();

            var result = await controller.Post(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value);
            Assert.DoesNotContain("senha", json, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetByOficinaId_DevePermitirAdminConsultarQualquerOficina()
        {
            var clientes = new[]
            {
                new ClienteOficinaDTO { Id = 1, Nome = "Cliente Oficina" }
            };
            _clienteServiceMock.Setup(s => s.GetByOficina(7)).ReturnsAsync(clientes);
            var controller = CreateController(roles: new[] { SystemRoles.Admin });

            var result = await controller.GetByOficinaId(7);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<Response>(ok.Value);
            Assert.Equal(ResponseEnum.SUCCESS, response.Code);
            Assert.Same(clientes, response.Data);
            _clienteServiceMock.Verify(s => s.GetByOficina(7), Times.Once);
        }

        [Fact]
        public async Task GetByOficinaId_DevePermitirOficinaConsultarPropriaOficina()
        {
            var clientes = new[]
            {
                new ClienteOficinaDTO { Id = 2, Nome = "Cliente Vinculado" }
            };
            _clienteServiceMock.Setup(s => s.GetByOficina(7)).ReturnsAsync(clientes);
            var controller = CreateController(roles: new[] { SystemRoles.Oficina }, oficinaId: 7);

            var result = await controller.GetByOficinaId(7);

            Assert.IsType<OkObjectResult>(result);
            _clienteServiceMock.Verify(s => s.GetByOficina(7), Times.Once);
        }

        [Fact]
        public async Task GetByOficinaId_DeveRetornarForbid_QuandoOficinaTentaConsultarOutraOficina()
        {
            var controller = CreateController(roles: new[] { SystemRoles.Oficina }, oficinaId: 7);

            var result = await controller.GetByOficinaId(8);

            Assert.IsType<ForbidResult>(result);
            _clienteServiceMock.Verify(s => s.GetByOficina(It.IsAny<int>()), Times.Never);
        }

        private ClienteController CreateController(int? userId = null, string[]? roles = null, int? oficinaId = null)
        {
            var controller = new ClienteController(
                _clienteServiceMock.Object,
                _mapperMock.Object,
                _passwordHasherMock.Object,
                _jwtTokenServiceMock.Object,
                _currentUserServiceMock.Object);

            _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
            _currentUserServiceMock.Setup(s => s.OficinaId).Returns(oficinaId);
            _currentUserServiceMock.Setup(s => s.IsInRole(It.IsAny<string>()))
                .Returns<string>(role => (roles ?? Array.Empty<string>()).Contains(role));
            return controller;
        }

        private static ClienteRequestDTO CriarClienteDto(int id = 0)
        {
            return new ClienteRequestDTO
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
