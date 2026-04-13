using System.Security.Claims;
using AutoMapper;
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
    public class VeiculoControllerTests
    {
        private readonly Mock<IVeiculoService> _veiculoServiceMock = new();
        private readonly Mock<IMapper> _mapperMock = new();

        [Fact]
        public async Task Get_DeveFiltrarVeiculosDoClienteLogado()
        {
            _veiculoServiceMock.Setup(s => s.GetAll()).ReturnsAsync(new List<VeiculoDTO>
            {
                CriarVeiculoDto(id: 1, clienteId: 5),
                CriarVeiculoDto(id: 2, clienteId: 8),
                CriarVeiculoDto(id: 3, clienteId: 5)
            });

            var controller = CreateController(userId: 5, SystemRoles.Cliente);

            var result = await controller.Get();

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<Response>(ok.Value);
            var data = Assert.IsAssignableFrom<IEnumerable<VeiculoDTO>>(response.Data);
            Assert.All(data, veiculo => Assert.Equal(5, veiculo.ClienteId));
            Assert.Equal(2, data.Count());
        }

        [Fact]
        public async Task Delete_DevePermitirFuncionarioExcluirVeiculo()
        {
            _veiculoServiceMock.Setup(s => s.Remove(4)).Returns(Task.CompletedTask);

            var controller = CreateController(userId: 10, SystemRoles.Funcionario);

            var result = await controller.Delete(4);

            Assert.IsType<OkObjectResult>(result);
            _veiculoServiceMock.Verify(s => s.Remove(4), Times.Once);
        }

        private VeiculoController CreateController(int? userId = null, params string[] roles)
        {
            var controller = new VeiculoController(_veiculoServiceMock.Object, _mapperMock.Object);
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
