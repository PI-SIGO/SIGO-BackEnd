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
    public class ClienteControllerTests
    {
        private readonly Mock<IClienteService> _clienteServiceMock = new();
        private readonly Mock<IClienteAuthenticationService> _clienteAuthenticationServiceMock = new();
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
        public async Task GetAll_DeveRetornarClientesAtivosEInativosDaOficina()
        {
            var clientes = new[]
            {
                new ClienteOficinaDTO
                {
                    Id = 1,
                    Nome = "Cliente ativo",
                    Situacao = (int)SIGO.Objects.Enums.Situacao.ATIVO
                },
                new ClienteOficinaDTO
                {
                    Id = 2,
                    Nome = "Cliente inativo",
                    Situacao = (int)SIGO.Objects.Enums.Situacao.INATIVO
                }
            };
            _clienteServiceMock.Setup(service => service.GetByOficina(7)).ReturnsAsync(clientes);
            var controller = CreateController(
                roles: new[] { SystemRoles.Oficina },
                oficinaId: 7);

            var result = await controller.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<PagedResponse<ClienteOficinaDTO>>(ok.Value);
            Assert.Equal(2, response.Items.Count);
            Assert.Contains(response.Items, cliente =>
                cliente.Situacao == (int)SIGO.Objects.Enums.Situacao.ATIVO);
            Assert.Contains(response.Items, cliente =>
                cliente.Situacao == (int)SIGO.Objects.Enums.Situacao.INATIVO);
            _clienteServiceMock.Verify(service => service.GetByOficina(7), Times.Once);
        }

        [Fact]
        public async Task Delete_DeveRetornarForbid_QuandoClienteTentaExcluirOutroCadastro()
        {
            var controller = CreateController(userId: 1, roles: new[] { SystemRoles.Cliente });

            var result = await controller.Delete(2);

            Assert.IsType<ForbidResult>(result);
            _clienteServiceMock.Verify(s => s.DeactivateAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Put_DevePermitir_QuandoClienteEditaProprioCadastro()
        {
            var dto = CriarClienteDto(id: 1);
            _clienteServiceMock.Setup(s => s.GetById(1)).ReturnsAsync(CriarClienteResponseDto(1));
            _clienteServiceMock.Setup(s => s.ValidarCpfCnpj(dto.Cpf_Cnpj, 1)).Returns(Task.CompletedTask);
            _clienteServiceMock.Setup(s => s.Update(It.IsAny<ClienteRequestDTO>(), 1)).Returns(Task.CompletedTask);

            var controller = CreateController(userId: 1, roles: new[] { SystemRoles.Cliente });

            var result = await controller.Put(1, dto);

            Assert.IsType<OkObjectResult>(result);
            _clienteServiceMock.Verify(s => s.Update(It.IsAny<ClienteRequestDTO>(), 1), Times.Once);
        }

        [Fact]
        public async Task UpdateForOficina_DeveEditarClienteDaOficinaDoJwt()
        {
            var request = CriarClienteDto(id: 7);
            request.senha = string.Empty;
            var response = new ClienteOficinaDTO { Id = 7, Nome = request.Nome };
            _clienteServiceMock
                .Setup(service => service.UpdateForOficina(request, 7, 2))
                .ReturnsAsync(response);
            var controller = CreateController(
                userId: 9,
                roles: new[] { SystemRoles.Oficina },
                oficinaId: 2);

            var result = await controller.UpdateForOficina(7, request);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Same(response, ok.Value);
            _clienteServiceMock.Verify(service => service.UpdateForOficina(
                request,
                7,
                2), Times.Once);
        }

        [Fact]
        public async Task UpdateForOficina_DeveRetornarForbid_QuandoJwtNaoTemOficinaId()
        {
            var controller = CreateController(
                userId: 9,
                roles: new[] { SystemRoles.Oficina });

            var result = await controller.UpdateForOficina(7, CriarClienteDto());

            Assert.IsType<ForbidResult>(result);
            _clienteServiceMock.Verify(service => service.UpdateForOficina(
                It.IsAny<ClienteRequestDTO>(),
                It.IsAny<int>(),
                It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void ClienteRequestDTO_NaoDeveExporSituacaoNoContratoDePerfil()
        {
            Assert.Null(typeof(ClienteRequestDTO).GetProperty("Situacao"));
        }

        [Fact]
        public async Task Login_DeveRetornarContratoBearerPadronizado()
        {
            var login = new LoginClienteDTO { Cpf = "52998224725", Senha = "Senha123" };
            _clienteAuthenticationServiceMock
                .Setup(service => service.AuthenticateAsync(login, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ClienteAuthenticationResult(
                    1,
                    "Cliente",
                    "cliente@test.com",
                    3));
            _jwtTokenServiceMock
                .Setup(service => service.GenerateToken(It.IsAny<JwtTokenRequest>()))
                .Returns("jwt-cliente");
            var controller = CreateController();

            var result = await controller.Login(login, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<AccessTokenResponse>(ok.Value);
            Assert.Equal("jwt-cliente", response.AccessToken);
            Assert.Equal("Bearer", response.TokenType);
        }

        [Fact]
        public async Task Delete_DeveInativarContaERetornarNoContent()
        {
            _clienteServiceMock.Setup(service => service.GetById(1))
                .ReturnsAsync(CriarClienteResponseDto(1));
            _clienteServiceMock.Setup(service => service.DeactivateAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            var controller = CreateController(userId: 1, roles: new[] { SystemRoles.Cliente });

            var result = await controller.Delete(1);

            Assert.IsType<NoContentResult>(result);
            _clienteServiceMock.Verify(service => service.DeactivateAsync(
                1,
                It.IsAny<CancellationToken>()), Times.Once);
            _clienteServiceMock.Verify(service => service.Remove(It.IsAny<int>()), Times.Never);
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
            var response = Assert.IsType<PagedResponse<ClienteOficinaDTO>>(ok.Value);
            Assert.Single(response.Items);
            Assert.Equal(clientes[0].Id, response.Items[0].Id);
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
                _clienteAuthenticationServiceMock.Object,
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

        private static ClienteDTO CriarClienteResponseDto(int id)
        {
            return new ClienteDTO
            {
                Id = id,
                Nome = "Cliente",
                Email = "cliente@test.com",
                Cpf_Cnpj = "12345678901",
                Cep = "12345678"
            };
        }
    }
}
