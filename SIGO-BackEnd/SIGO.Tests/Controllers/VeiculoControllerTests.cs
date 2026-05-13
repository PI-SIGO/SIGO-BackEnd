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
    public class VeiculoControllerTests
    {
        private readonly Mock<IVeiculoService> _veiculoServiceMock = new();
        private readonly Mock<IMapper> _mapperMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();

        [Fact]
        public async Task Get_DeveFiltrarVeiculosDoClienteLogado()
        {
            _veiculoServiceMock.Setup(s => s.GetByCliente(5)).ReturnsAsync(new List<VeiculoDTO>
            {
                CriarVeiculoDto(id: 1, clienteId: 5),
                CriarVeiculoDto(id: 3, clienteId: 5)
            });

            var controller = CreateController(userId: 5, roles: new[] { SystemRoles.Cliente });

            var result = await controller.Get();

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<Response>(ok.Value);
            var data = Assert.IsAssignableFrom<IEnumerable<VeiculoDTO>>(response.Data);
            Assert.All(data, veiculo => Assert.Equal(5, veiculo.ClienteId));
            Assert.Equal(2, data.Count());
            _veiculoServiceMock.Verify(s => s.GetAll(), Times.Never);
        }

        [Fact]
        public async Task Delete_DeveRetornarForbid_QuandoFuncionarioTentaExcluirVeiculoGlobal()
        {
            var controller = CreateController(userId: 10, oficinaId: 2, roles: new[] { SystemRoles.Funcionario });

            var result = await controller.Delete(4);

            Assert.IsType<ForbidResult>(result);
            _veiculoServiceMock.Verify(s => s.Remove(It.IsAny<int>()), Times.Never);
            _veiculoServiceMock.Verify(s => s.GetByIdForOficina(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        private VeiculoController CreateController(int? userId = null, int? oficinaId = null, params string[] roles)
        {
            var controller = new VeiculoController(
                _veiculoServiceMock.Object,
                _mapperMock.Object,
                _currentUserServiceMock.Object);

            _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
            _currentUserServiceMock.Setup(s => s.OficinaId).Returns(oficinaId);
            _currentUserServiceMock.Setup(s => s.IsInRole(It.IsAny<string>()))
                .Returns<string>(role => roles.Contains(role));
            return controller;
        }

        private static VeiculoDTO CriarVeiculoDto(int id, int clienteId)
        {
            return new VeiculoDTO
            {
                Id = id,
                NomeVeiculo = "Carro",
                TipoVeiculo = "Hatch",
                PlacaVeiculo = "ABC1234",
                ChassiVeiculo = $"CHASSI{id}",
                AnoFab = 2020,
                Quilometragem = 10000,
                Combustivel = "Gasolina",
                Seguro = "Ativo",
                Cor = "Preto",
                ClienteId = clienteId
            };
        }
    }
}
