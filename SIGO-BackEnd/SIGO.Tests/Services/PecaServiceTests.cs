using AutoMapper;
using Moq;
using SIGO.Data.Interfaces;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;
using SIGO.Services.Entities;
using SIGO.Validation;
using Xunit;

namespace SIGO.Tests.Services
{
    public class PecaServiceTests
    {
        [Fact]
        public async Task Create_PreencheIdPersistido()
        {
            var repository = new Mock<IPecaRepository>();
            var mapper = new Mock<IMapper>();
            var request = CreateRequest(oficinaId: 3);
            var entity = new Peca();
            mapper.Setup(item => item.Map<Peca>(request)).Returns(entity);
            repository.Setup(item => item.Add(entity))
                .Callback(() => entity.Id = 18)
                .Returns(Task.CompletedTask);
            var service = new PecaService(repository.Object, mapper.Object);

            await service.Create(request);

            Assert.Equal(18, request.Id);
        }

        [Fact]
        public async Task Create_AdminRejeitaPecaSemOficina()
        {
            var service = new PecaService(Mock.Of<IPecaRepository>(), Mock.Of<IMapper>());

            var exception = await Assert.ThrowsAsync<BusinessValidationException>(
                () => service.Create(CreateRequest(oficinaId: null)));

            Assert.Contains(exception.Errors, error => error.Field == nameof(PecaDTO.IdOficina));
        }

        [Fact]
        public async Task Update_AtualizaQuantidadeEmEstoque()
        {
            var repository = new Mock<IPecaRepository>();
            var existing = new Peca
            {
                Id = 7,
                IdOficina = 3,
                QuantidadeEstoque = 5
            };
            var request = CreateRequest(oficinaId: 3);
            request.QuantidadeEstoque = 30;
            repository.Setup(item => item.GetById(7)).ReturnsAsync(existing);
            repository.Setup(item => item.SaveChanges()).ReturnsAsync(1);
            var service = new PecaService(repository.Object, Mock.Of<IMapper>());

            await service.Update(request, 7);

            Assert.Equal(30, existing.QuantidadeEstoque);
            repository.Verify(item => item.SaveChanges(), Times.Once);
        }

        private static PecaDTO CreateRequest(int? oficinaId) => new()
        {
            Nome = "Filtro",
            Tipo = "Oleo",
            Descricao = "Filtro",
            Valor = 50,
            Quantidade = 1,
            QuantidadeEstoque = 30,
            Garantia = DateOnly.FromDateTime(DateTime.Today),
            Unidade = 1,
            IdMarca = 1,
            DataAquisicao = DateOnly.FromDateTime(DateTime.Today),
            Fornecedor = "Fornecedor",
            IdOficina = oficinaId
        };
    }
}
