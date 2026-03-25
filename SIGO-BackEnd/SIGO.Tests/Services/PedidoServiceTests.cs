using AutoMapper;
using Moq;
using SIGO.Data.Interfaces;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;
using SIGO.Services.Entities;
using Xunit;

namespace SIGO.Tests.Services
{
    public class PedidoServiceTests
    {
        private readonly Mock<IPedidoRepository> _repositoryMock = new();
        private readonly Mock<IMapper> _mapperMock = new();

        [Fact]
        public async Task Create_DeveMapearEAdicionarEntidade()
        {
            var dto = new PedidoDTO { Id = 7, Observacao = "novo" };
            var entity = new Pedido();

            _mapperMock.Setup(m => m.Map<Pedido>(dto)).Returns(entity);

            var service = new PedidoService(_repositoryMock.Object, _mapperMock.Object);

            await service.Create(dto);

            _repositoryMock.Verify(r => r.Add(entity), Times.Once);
        }

        [Fact]
        public async Task Update_DeveForcarIdDoDtoEAtualizar()
        {
            var id = 15;
            var dto = new PedidoDTO { Id = 0, Observacao = "update" };

            _repositoryMock.Setup(r => r.GetById(id)).ReturnsAsync(new Pedido());
            _mapperMock.Setup(m => m.Map<Pedido>(It.IsAny<PedidoDTO>()))
                .Returns<PedidoDTO>(source => new Pedido { Id = source.Id });

            var service = new PedidoService(_repositoryMock.Object, _mapperMock.Object);

            await service.Update(dto, id);

            Assert.Equal(id, dto.Id);
            _repositoryMock.Verify(r => r.Update(It.Is<Pedido>(p => p.Id == id)), Times.Once);
        }

        [Fact]
        public async Task Update_DeveLancarExcecao_QuandoPedidoNaoExiste()
        {
            var id = 20;
            var dto = new PedidoDTO();

            _repositoryMock.Setup(r => r.GetById(id)).ReturnsAsync((Pedido)null!);

            var service = new PedidoService(_repositoryMock.Object, _mapperMock.Object);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.Update(dto, id));
        }
    }
}
