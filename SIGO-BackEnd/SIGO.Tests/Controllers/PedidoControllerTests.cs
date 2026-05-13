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
    public class PedidoControllerTests
    {
        private readonly Mock<IPedidoService> _pedidoServiceMock = new();
        private readonly Mock<IServicoService> _servicoServiceMock = new();
        private readonly Mock<IFuncionarioService> _funcionarioServiceMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();

        [Fact]
        public async Task GetById_DeveRetornarNotFound_QuandoPedidoNaoExiste()
        {
            _pedidoServiceMock.Setup(s => s.GetById(1))
                .ReturnsAsync((PedidoDTO)null!);

            var controller = CreateController();

            var result = await controller.GetById(1);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<Response>(notFound.Value);
            Assert.Equal(ResponseEnum.NOT_FOUND, response.Code);
        }

        [Fact]
        public async Task GetById_DeveRetornarForbid_QuandoClienteTentaAcessarPedidoDeOutroCliente()
        {
            _pedidoServiceMock.Setup(s => s.GetById(10))
                .ReturnsAsync(CriarPedidoDto(idCliente: 99));

            var controller = CreateController(userId: 5, roles: SystemRoles.Cliente);

            var result = await controller.GetById(10);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task GetAll_DeveFiltrarPedidosDoClienteLogado()
        {
            var pedidos = new List<PedidoDTO>
            {
                CriarPedidoDto(id: 1, idCliente: 5),
                CriarPedidoDto(id: 2, idCliente: 9),
                CriarPedidoDto(id: 3, idCliente: 5)
            };

            _pedidoServiceMock.Setup(s => s.GetByCliente(5)).ReturnsAsync(pedidos.Where(p => p.idCliente == 5));

            var controller = CreateController(userId: 5, roles: SystemRoles.Cliente);

            var result = await controller.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<Response>(ok.Value);
            var data = Assert.IsAssignableFrom<IEnumerable<PedidoDTO>>(response.Data);
            Assert.All(data, pedido => Assert.Equal(5, pedido.idCliente));
            Assert.Equal(2, data.Count());
        }

        [Fact]
        public async Task GetMyServices_DeveRetornarSomenteServicosDoClienteLogado()
        {
            _pedidoServiceMock.Setup(s => s.GetByCliente(5)).ReturnsAsync(new List<PedidoDTO>
            {
                CriarPedidoDto(idCliente: 5, servicoIds: new[] { 1, 2 }),
                CriarPedidoDto(idCliente: 5, servicoIds: new[] { 2, 3 }),
            });

            _servicoServiceMock.Setup(s => s.GetAll()).ReturnsAsync(new List<ServicoDTO>
            {
                new() { Id = 1, Nome = "Troca de oleo" },
                new() { Id = 2, Nome = "Alinhamento" },
                new() { Id = 3, Nome = "Balanceamento" },
                new() { Id = 4, Nome = "Lavagem" }
            });

            var controller = CreateController(userId: 5, roles: SystemRoles.Cliente);

            var result = await controller.GetMyServices();

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsAssignableFrom<IEnumerable<ServicoDTO>>(ok.Value);
            Assert.Equal(new[] { 1, 2, 3 }, data.Select(x => x.Id).OrderBy(x => x));
        }

        [Fact]
        public async Task GetMyEmployees_DeveRetornarSomenteFuncionariosRelacionadosAoClienteLogado()
        {
            _pedidoServiceMock.Setup(s => s.GetByCliente(5)).ReturnsAsync(new List<PedidoDTO>
            {
                CriarPedidoDto(idCliente: 5, idFuncionario: 10),
                CriarPedidoDto(idCliente: 5, idFuncionario: 11),
            });

            _funcionarioServiceMock.Setup(s => s.GetAll()).ReturnsAsync(new List<FuncionarioDTO>
            {
                new() { Id = 10, Nome = "Funcionario 10" },
                new() { Id = 11, Nome = "Funcionario 11" },
                new() { Id = 12, Nome = "Funcionario 12" }
            });

            var controller = CreateController(userId: 5, roles: SystemRoles.Cliente);

            var result = await controller.GetMyEmployees();

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsAssignableFrom<IEnumerable<FuncionarioDTO>>(ok.Value);
            Assert.Equal(new[] { 10, 11 }, data.Select(x => x.Id).OrderBy(x => x));
        }

        [Fact]
        public async Task Post_DeveRetornarBadRequest_QuandoDtoForNulo()
        {
            var controller = CreateController();

            var result = await controller.Post(null!);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<Response>(badRequest.Value);
            Assert.Equal(ResponseEnum.INVALID, response.Code);
        }

        [Fact]
        public async Task Post_DeveZerarIdEChamarCreate_QuandoDtoValido()
        {
            var dto = CriarPedidoDto();
            dto.Id = 99;

            PedidoDTO? dtoRecebido = null;
            _pedidoServiceMock.Setup(s => s.Create(It.IsAny<PedidoDTO>()))
                .Callback<PedidoDTO>(d => dtoRecebido = d)
                .Returns(Task.CompletedTask);

            var controller = CreateController();

            var result = await controller.Post(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<Response>(ok.Value);
            Assert.Equal(ResponseEnum.SUCCESS, response.Code);

            Assert.NotNull(dtoRecebido);
            Assert.Equal(0, dtoRecebido!.Id);
            _pedidoServiceMock.Verify(s => s.Create(It.IsAny<PedidoDTO>()), Times.Once);
        }

        [Fact]
        public async Task Delete_DeveRemover_QuandoPedidoExiste()
        {
            _pedidoServiceMock.Setup(s => s.GetById(5))
                .ReturnsAsync(CriarPedidoDto());
            _pedidoServiceMock.Setup(s => s.Remove(5))
                .Returns(Task.CompletedTask);

            var controller = CreateController();

            var result = await controller.Delete(5);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<Response>(ok.Value);
            Assert.Equal(ResponseEnum.SUCCESS, response.Code);
            _pedidoServiceMock.Verify(s => s.Remove(5), Times.Once);
        }

        private PedidoController CreateController(int? userId = null, params string[] roles)
        {
            var controller = new PedidoController(
                _pedidoServiceMock.Object,
                _servicoServiceMock.Object,
                _funcionarioServiceMock.Object,
                _currentUserServiceMock.Object);

            _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
            _currentUserServiceMock.Setup(s => s.IsInRole(It.IsAny<string>()))
                .Returns<string>(role => roles.Contains(role));
            return controller;
        }

        private static PedidoDTO CriarPedidoDto(
            int id = 1,
            int idCliente = 1,
            int idFuncionario = 1,
            int[]? servicoIds = null)
        {
            return new PedidoDTO
            {
                Id = id,
                idCliente = idCliente,
                idFuncionario = idFuncionario,
                idOficina = 1,
                idVeiculo = 1,
                ValorTotal = 100,
                DescontoReais = 0,
                DescontoPorcentagem = 0,
                DescontoTotalReais = 0,
                DescontoServicoPorcentagem = 0,
                DescontoServicoReais = 0,
                DescontoPecaPorcentagem = 0,
                descontoPecaReais = 0,
                Observacao = "teste",
                DataInicio = DateOnly.FromDateTime(DateTime.Today),
                DataFim = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                Pedido_Servicos = (servicoIds ?? Array.Empty<int>()).Select(idServico => new Pedido_ServicoDTO
                {
                    IdPedido = id,
                    IdServico = idServico,
                    QuantVezes = 1
                }).ToList()
            };
        }
    }
}
