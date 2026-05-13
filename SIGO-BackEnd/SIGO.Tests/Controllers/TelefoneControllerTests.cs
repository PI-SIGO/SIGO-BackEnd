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
            _telefoneServiceMock.Setup(s => s.Create(It.IsAny<TelefoneDTO>())).Returns(Task.CompletedTask);

            var controller = CreateController(userId: 5, SystemRoles.Cliente);

            var result = await controller.Post(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<Response>(ok.Value);
            Assert.Equal(ResponseEnum.SUCCESS, response.Code);
            _telefoneServiceMock.Verify(s => s.Create(It.Is<TelefoneDTO>(t => t.ClienteId == 5)), Times.Once);
        }

        [Fact]
        public async Task Post_DeveRetornarForbid_QuandoClienteTentaCadastrarTelefoneDeOutroCliente()
        {
            var dto = CriarTelefoneDto(clienteId: 9);
            var controller = CreateController(userId: 5, SystemRoles.Cliente);

            var result = await controller.Post(dto);

            Assert.IsType<ForbidResult>(result);
            _telefoneServiceMock.Verify(s => s.Create(It.IsAny<TelefoneDTO>()), Times.Never);
        }

        [Fact]
        public async Task Delete_DeveRetornarForbid_QuandoClienteTentaExcluirTelefoneDeOutroCliente()
        {
            _telefoneServiceMock.Setup(s => s.GetById(10)).ReturnsAsync(CriarTelefoneDto(id: 10, clienteId: 9));
            var controller = CreateController(userId: 5, SystemRoles.Cliente);

            var result = await controller.Delete(10);

            Assert.IsType<ForbidResult>(result);
            _telefoneServiceMock.Verify(s => s.Remove(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Put_DevePermitirClienteAtualizarProprioTelefone()
        {
            var dto = CriarTelefoneDto(id: 3, clienteId: 5);
            _telefoneServiceMock.Setup(s => s.GetById(3)).ReturnsAsync(CriarTelefoneDto(id: 3, clienteId: 5));
            _telefoneServiceMock.Setup(s => s.Update(It.IsAny<TelefoneDTO>(), 3)).Returns(Task.CompletedTask);

            var controller = CreateController(userId: 5, SystemRoles.Cliente);

            var result = await controller.Put(3, dto);

            Assert.IsType<OkObjectResult>(result);
            _telefoneServiceMock.Verify(s => s.Update(It.Is<TelefoneDTO>(t => t.ClienteId == 5), 3), Times.Once);
        }

        private TelefoneController CreateController(int? userId = null, params string[] roles)
        {
            var controller = new TelefoneController(
                _telefoneServiceMock.Object,
                _clienteServiceMock.Object,
                _mapperMock.Object,
                _currentUserServiceMock.Object);

            _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
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
