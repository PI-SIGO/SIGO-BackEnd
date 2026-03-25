using Microsoft.AspNetCore.Mvc;
using Moq;
using SIGO.Controllers;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Services.Interfaces;
using Xunit;

namespace SIGO.Tests.Controllers
{
    public class PedidoControllerTests
    {
        private readonly Mock<IPedidoService> _pedidoServiceMock = new();

        [Fact]
        public async Task GetById_DeveRetornarNotFound_QuandoPedidoNaoExiste()
        {   
            _pedidoServiceMock.Setup(s => s.GetById(1))
                .ReturnsAsync((PedidoDTO)null!);

            var controller = new PedidoController(_pedidoServiceMock.Object);

            var result = await controller.GetById(1);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<Response>(notFound.Value);
            Assert.Equal(ResponseEnum.NOT_FOUND, response.Code);
        }

        [Fact]
        public async Task Post_DeveRetornarBadRequest_QuandoDtoForNulo()
        {
            var controller = new PedidoController(_pedidoServiceMock.Object);

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

            var controller = new PedidoController(_pedidoServiceMock.Object);

            var result = await controller.Post(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<Response>(ok.Value);
            Assert.Equal(ResponseEnum.SUCCESS, response.Code);

            Assert.NotNull(dtoRecebido);
            Assert.Equal(0, dtoRecebido!.Id);
            _pedidoServiceMock.Verify(s => s.Create(It.IsAny<PedidoDTO>()), Times.Once);
        }

        [Theory]
        [InlineData("'; DROP TABLE pedido; --")]
        [InlineData("<script>alert('xss')</script>")]
        [InlineData("../../../../windows/system32")]
        [InlineData("$(rm -rf /)")]
        public async Task Post_DeveProcessarPayloadMaliciosoSemFalha(string payload)
        {
            var dto = CriarPedidoDto();
            dto.Observacao = payload;

            _pedidoServiceMock.Setup(s => s.Create(It.IsAny<PedidoDTO>()))
                .Returns(Task.CompletedTask);

            var controller = new PedidoController(_pedidoServiceMock.Object); 

            var result = await controller.Post(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<Response>(ok.Value);
            Assert.Equal(ResponseEnum.SUCCESS, response.Code);
            _pedidoServiceMock.Verify(s => s.Create(It.Is<PedidoDTO>(d => d.Observacao == payload)), Times.Once);
        }

        [Fact]
        public async Task Post_EmErroInterno_NaoDeveExporDetalhesSensiveis()
        {
            var dto = CriarPedidoDto();
            _pedidoServiceMock.Setup(s => s.Create(It.IsAny<PedidoDTO>()))
                .ThrowsAsync(new Exception("falha interna"));

            var controller = new PedidoController(_pedidoServiceMock.Object);

            var result = await controller.Post(dto);

            var erro = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, erro.StatusCode);

            var response = Assert.IsType<Response>(erro.Value);
            Assert.Equal(ResponseEnum.ERROR, response.Code);
            Assert.Null(response.Data);
        }

        [Fact]
        public async Task Put_NaoDeveAtualizar_QuandoPedidoNaoExiste()
        {
            var dto = CriarPedidoDto();
            _pedidoServiceMock.Setup(s => s.GetById(10))
                .ReturnsAsync((PedidoDTO)null!);

            var controller = new PedidoController(_pedidoServiceMock.Object);

            var result = await controller.Put(10, dto);

            Assert.IsType<NotFoundObjectResult>(result);
            _pedidoServiceMock.Verify(s => s.Update(It.IsAny<PedidoDTO>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Delete_DeveRemover_QuandoPedidoExiste()
        {
            _pedidoServiceMock.Setup(s => s.GetById(5))
                .ReturnsAsync(CriarPedidoDto());
            _pedidoServiceMock.Setup(s => s.Remove(5))
                .Returns(Task.CompletedTask);

            var controller = new PedidoController(_pedidoServiceMock.Object);

            var result = await controller.Delete(5);

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<Response>(ok.Value);
            Assert.Equal(ResponseEnum.SUCCESS, response.Code);
            _pedidoServiceMock.Verify(s => s.Remove(5), Times.Once);
        }

        private static PedidoDTO CriarPedidoDto() => new()
        {
            Id = 1,
            idCliente = 1,
            idFuncionario = 1,
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
            DataFim = DateOnly.FromDateTime(DateTime.Today.AddDays(1))
        };
    }
}
