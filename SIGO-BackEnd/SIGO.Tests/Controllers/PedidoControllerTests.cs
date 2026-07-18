using Microsoft.AspNetCore.Authorization;
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
    public class PedidoControllerTests
    {
        private readonly Mock<IPedidoService> _pedidoService = new();
        private readonly Mock<IServicoService> _servicoService = new();
        private readonly Mock<IFuncionarioService> _funcionarioService = new();
        private readonly Mock<ICurrentUserService> _currentUser = new();

        [Fact]
        public async Task GetById_RetornaNotFound_QuandoPedidoNaoExiste()
        {
            _pedidoService.Setup(service => service.GetById(1)).ReturnsAsync((PedidoDTO)null!);

            var result = await CreateController(roles: SystemRoles.Admin).GetById(1);

            var notFound = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
            Assert.IsType<ProblemDetails>(notFound.Value);
        }

        [Fact]
        public async Task GetById_RetornaForbid_QuandoClienteNaoEProprietario()
        {
            _pedidoService.Setup(service => service.GetById(10))
                .ReturnsAsync(CreateOrder(clienteId: 99));

            var result = await CreateController(userId: 5, roles: SystemRoles.Cliente).GetById(10);

            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task GetAll_AplicaEscopoDoClienteEPaginacao()
        {
            _pedidoService.Setup(service => service.GetByCliente(5)).ReturnsAsync(new[]
            {
                CreateOrder(id: 1, clienteId: 5),
                CreateOrder(id: 2, clienteId: 5),
                CreateOrder(id: 3, clienteId: 5)
            });

            var result = await CreateController(userId: 5, roles: SystemRoles.Cliente)
                .GetAll(new PaginationRequest { Page = 2, PageSize = 2 });

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var page = Assert.IsType<PagedResponse<PedidoDTO>>(ok.Value);
            Assert.Equal(3, page.TotalItems);
            Assert.Equal(2, page.TotalPages);
            Assert.Equal(3, Assert.Single(page.Items).Id);
            Assert.All(page.Items, order => Assert.Equal(5, order.idCliente));
        }

        [Fact]
        public async Task GetAll_FuncionarioConsultaSomentePedidosDaPropriaOficina()
        {
            _pedidoService.Setup(service => service.GetByOficina(7))
                .ReturnsAsync(new[] { CreateOrder(id: 1) });

            var result = await CreateController(oficinaId: 7, roles: SystemRoles.Funcionario)
                .GetAll(new PaginationRequest());

            Assert.IsType<OkObjectResult>(result.Result);
            _pedidoService.Verify(service => service.GetByOficina(7), Times.Once);
            _pedidoService.Verify(service => service.GetAll(), Times.Never);
        }

        [Fact]
        public async Task GetById_FuncionarioConsultaPedidoNoEscopoDaPropriaOficina()
        {
            var order = CreateOrder(id: 10);
            _pedidoService.Setup(service => service.GetByIdForOficina(10, 7)).ReturnsAsync(order);

            var result = await CreateController(oficinaId: 7, roles: SystemRoles.Funcionario)
                .GetById(10);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(order, ok.Value);
            _pedidoService.Verify(service => service.GetByIdForOficina(10, 7), Times.Once);
            _pedidoService.Verify(service => service.GetById(10), Times.Never);
        }

        [Fact]
        public async Task GetMyServices_RetornaSomenteServicosDosPedidos()
        {
            _pedidoService.Setup(service => service.GetByCliente(5)).ReturnsAsync(new[]
            {
                CreateOrder(clienteId: 5, serviceIds: new[] { 1, 2 }),
                CreateOrder(clienteId: 5, serviceIds: new[] { 2, 3 })
            });
            _servicoService.Setup(service => service.GetAll()).ReturnsAsync(new[]
            {
                new ServicoDTO { Id = 1 },
                new ServicoDTO { Id = 2 },
                new ServicoDTO { Id = 3 },
                new ServicoDTO { Id = 4 }
            });

            var result = await CreateController(userId: 5, roles: SystemRoles.Cliente)
                .GetMyServices(new PaginationRequest());

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var page = Assert.IsType<PagedResponse<ServicoDTO>>(ok.Value);
            Assert.Equal(new[] { 1, 2, 3 }, page.Items.Select(item => item.Id).OrderBy(id => id));
        }

        [Fact]
        public async Task GetMyEmployees_RetornaSomenteFuncionariosDosPedidos()
        {
            _pedidoService.Setup(service => service.GetByCliente(5)).ReturnsAsync(new[]
            {
                CreateOrder(clienteId: 5, funcionarioId: 10),
                CreateOrder(clienteId: 5, funcionarioId: 11)
            });
            _funcionarioService.Setup(service => service.GetAll()).ReturnsAsync(new[]
            {
                new FuncionarioDTO { Id = 10 },
                new FuncionarioDTO { Id = 11 },
                new FuncionarioDTO { Id = 12 }
            });

            var result = await CreateController(userId: 5, roles: SystemRoles.Cliente)
                .GetMyEmployees(new PaginationRequest());

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var page = Assert.IsType<PagedResponse<FuncionarioDTO>>(ok.Value);
            Assert.Equal(new[] { 10, 11 }, page.Items.Select(item => item.Id).OrderBy(id => id));
        }

        [Fact]
        public async Task Post_RetornaCreatedComRotaOficial()
        {
            var request = CreateOrder(id: 87);
            _pedidoService.Setup(service => service.Create(request))
                .Callback(() => request.Id = 25)
                .Returns(Task.CompletedTask);

            var result = await CreateController(roles: SystemRoles.Admin).Post(request);

            var created = Assert.IsType<CreatedResult>(result.Result);
            Assert.Equal("/api/v1/pedidos/25", created.Location);
            Assert.Same(request, created.Value);
        }

        [Fact]
        public async Task Post_FuncionarioCriaPedidoNoEscopoDaPropriaOficina()
        {
            var request = CreateOrder();
            request.idOficina = 99;
            _pedidoService.Setup(service => service.CreateForOficina(request, 7))
                .Returns(Task.CompletedTask);

            var result = await CreateController(oficinaId: 7, roles: SystemRoles.Funcionario)
                .Post(request);

            Assert.IsType<CreatedResult>(result.Result);
            _pedidoService.Verify(service => service.CreateForOficina(request, 7), Times.Once);
            _pedidoService.Verify(service => service.Create(It.IsAny<PedidoDTO>()), Times.Never);
        }

        [Fact]
        public async Task Put_FuncionarioAtualizaPedidoNoEscopoDaPropriaOficina()
        {
            var request = CreateOrder(id: 10);
            _pedidoService.Setup(service => service.UpdateForOficina(request, 10, 7))
                .Returns(Task.CompletedTask);

            var result = await CreateController(oficinaId: 7, roles: SystemRoles.Funcionario)
                .Put(10, request);

            Assert.IsType<OkObjectResult>(result.Result);
            _pedidoService.Verify(service => service.UpdateForOficina(request, 10, 7), Times.Once);
            _pedidoService.Verify(service => service.Update(It.IsAny<PedidoDTO>(), It.IsAny<int>()), Times.Never);
        }

        [Theory]
        [InlineData(nameof(PedidoController.GetAll))]
        [InlineData(nameof(PedidoController.GetById))]
        [InlineData(nameof(PedidoController.Post))]
        [InlineData(nameof(PedidoController.Put))]
        public void OperacoesDePedido_DevemAutorizarFuncionario(string methodName)
        {
            var attribute = Assert.Single(typeof(PedidoController)
                .GetMethod(methodName)!
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
                .Cast<AuthorizeAttribute>());

            Assert.Contains(SystemRoles.Funcionario, attribute.Roles ?? string.Empty);
        }

        [Fact]
        public void Delete_DeveContinuarRestritoAAdministracaoDaOficina()
        {
            var attribute = Assert.Single(typeof(PedidoController)
                .GetMethod(nameof(PedidoController.Delete))!
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
                .Cast<AuthorizeAttribute>());

            Assert.DoesNotContain(SystemRoles.Funcionario, attribute.Roles ?? string.Empty);
        }

        [Fact]
        public async Task Delete_RetornaNoContent_QuandoPedidoExiste()
        {
            _pedidoService.Setup(service => service.GetById(5)).ReturnsAsync(CreateOrder(id: 5));
            _pedidoService.Setup(service => service.Remove(5)).Returns(Task.CompletedTask);

            var result = await CreateController(roles: SystemRoles.Admin).Delete(5);

            Assert.IsType<NoContentResult>(result);
            _pedidoService.Verify(service => service.Remove(5), Times.Once);
        }

        private PedidoController CreateController(
            int? userId = null,
            int? oficinaId = null,
            params string[] roles)
        {
            _currentUser.Setup(user => user.UserId).Returns(userId);
            _currentUser.Setup(user => user.OficinaId).Returns(oficinaId);
            _currentUser.Setup(user => user.IsInRole(It.IsAny<string>()))
                .Returns<string>(role => roles.Contains(role));

            return new PedidoController(
                _pedidoService.Object,
                _servicoService.Object,
                _funcionarioService.Object,
                _currentUser.Object);
        }

        private static PedidoDTO CreateOrder(
            int id = 1,
            int clienteId = 1,
            int funcionarioId = 1,
            int[]? serviceIds = null)
        {
            return new PedidoDTO
            {
                Id = id,
                idCliente = clienteId,
                idFuncionario = funcionarioId,
                idOficina = 1,
                idVeiculo = 1,
                Observacao = "teste",
                DataInicio = DateOnly.FromDateTime(DateTime.Today),
                DataFim = DateOnly.FromDateTime(DateTime.Today),
                Pedido_Servicos = (serviceIds ?? Array.Empty<int>())
                    .Select(serviceId => new Pedido_ServicoDTO
                    {
                        IdPedido = id,
                        IdServico = serviceId,
                        QuantVezes = 1
                    })
                    .ToList()
            };
        }
    }
}
