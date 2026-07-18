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
    public class TelefoneControllerTests
    {
        private readonly Mock<ITelefoneService> _telefoneServiceMock = new();
        private readonly Mock<IClienteService> _clienteServiceMock = new();
        private readonly Mock<IMapper> _mapperMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();

        [Fact]
        public async Task Post_DevePermitirClienteCadastrarProprioTelefone()
        {
            var dto = CriarTelefoneDto(clienteId: 5);
            _telefoneServiceMock.Setup(s => s.CreateTelefone(It.IsAny<TelefoneDTO>())).ReturnsAsync(dto);

            var controller = CreateController(userId: 5, roles: new[] { SystemRoles.Cliente });

            var result = await controller.Post(dto);

            var created = Assert.IsType<CreatedResult>(result);
            Assert.Same(dto, created.Value);
            _telefoneServiceMock.Verify(s => s.CreateTelefone(It.Is<TelefoneDTO>(t => t.ClienteId == 5)), Times.Once);
        }

        [Fact]
        public async Task Post_DeveRetornarForbid_QuandoClienteTentaCadastrarTelefoneDeOutroCliente()
        {
            var dto = CriarTelefoneDto(clienteId: 9);
            var controller = CreateController(userId: 5, roles: new[] { SystemRoles.Cliente });

            var result = await controller.Post(dto);

            Assert.IsType<ForbidResult>(result);
            _telefoneServiceMock.Verify(s => s.CreateTelefone(It.IsAny<TelefoneDTO>()), Times.Never);
        }

        [Fact]
        public async Task Delete_DeveRetornarForbid_QuandoClienteTentaExcluirTelefoneDeOutroCliente()
        {
            _telefoneServiceMock.Setup(s => s.GetById(10)).ReturnsAsync(CriarTelefoneDto(id: 10, clienteId: 9));
            var controller = CreateController(userId: 5, roles: new[] { SystemRoles.Cliente });

            var result = await controller.Delete(10);

            Assert.IsType<ForbidResult>(result);
            _telefoneServiceMock.Verify(s => s.Remove(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Put_DevePermitirClienteAtualizarProprioTelefone()
        {
            var dto = CriarTelefoneDto(id: 3, clienteId: 5);
            _telefoneServiceMock.Setup(s => s.GetById(3)).ReturnsAsync(CriarTelefoneDto(id: 3, clienteId: 5));
            _telefoneServiceMock.Setup(s => s.UpdateTelefone(It.IsAny<TelefoneDTO>(), 3)).ReturnsAsync(dto);

            var controller = CreateController(userId: 5, roles: new[] { SystemRoles.Cliente });

            var result = await controller.Put(3, dto);

            Assert.IsType<OkObjectResult>(result);
            _telefoneServiceMock.Verify(s => s.UpdateTelefone(It.Is<TelefoneDTO>(t => t.ClienteId == 5), 3), Times.Once);
        }

        [Fact]
        public async Task GetByName_DeveRetornarPaginaEscopadaDaOficina()
        {
            var telefones = new[]
            {
                CriarTelefoneDto(id: 1, clienteId: 5),
                CriarTelefoneDto(id: 2, clienteId: 6)
            };
            _telefoneServiceMock
                .Setup(s => s.GetTelefoneByNomeForOficina("Maria", 7))
                .ReturnsAsync(telefones);
            var controller = CreateController(oficinaId: 7, roles: new[] { SystemRoles.Oficina });

            var result = await controller.GetByNameWithDetails(
                "Maria",
                new PaginationRequest { Page = 1, PageSize = 1 });

            var ok = Assert.IsType<OkObjectResult>(result);
            var page = Assert.IsType<PagedResponse<TelefoneDTO>>(ok.Value);
            Assert.Single(page.Items);
            Assert.Equal(2, page.TotalItems);
        }

        [Fact]
        public async Task Delete_DeveRetornarNoContent_QuandoClienteExcluiTelefoneProprio()
        {
            _telefoneServiceMock.Setup(s => s.GetById(10)).ReturnsAsync(CriarTelefoneDto(id: 10, clienteId: 5));
            var controller = CreateController(userId: 5, roles: new[] { SystemRoles.Cliente });

            var result = await controller.Delete(10);

            Assert.IsType<NoContentResult>(result);
            _telefoneServiceMock.Verify(s => s.Remove(10), Times.Once);
        }

        [Fact]
        public async Task GetByName_DeveRetornarPaginaVazia_ParaPesquisaSemResultado()
        {
            _telefoneServiceMock.Setup(s => s.GetTelefoneByNome("ninguém")).ReturnsAsync(Array.Empty<TelefoneDTO>());
            var controller = CreateController(roles: new[] { SystemRoles.Admin });

            var result = await controller.GetByNameWithDetails("ninguém");

            var ok = Assert.IsType<OkObjectResult>(result);
            var page = Assert.IsType<PagedResponse<TelefoneDTO>>(ok.Value);
            Assert.Empty(page.Items);
        }

        private TelefoneController CreateController(
            int? userId = null,
            int? oficinaId = null,
            params string[] roles)
        {
            var controller = new TelefoneController(
                _telefoneServiceMock.Object,
                _clienteServiceMock.Object,
                _mapperMock.Object,
                _currentUserServiceMock.Object);

            _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
            _currentUserServiceMock.Setup(s => s.OficinaId).Returns(oficinaId);
            _currentUserServiceMock.Setup(s => s.IsInRole(It.IsAny<string>()))
                .Returns<string>(role => roles.Contains(role));
            return controller;
        }

        private static TelefoneDTO CriarTelefoneDto(int id = 1, int clienteId = 1)
        {
            return new TelefoneDTO
            {
                Id = id,
                Numero = "11999999999",
                DDD = 11,
                ClienteId = clienteId
            };
        }
    }
}
