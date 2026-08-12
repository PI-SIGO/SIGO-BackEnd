using AutoMapper;
using Moq;
using SIGO.Data.Interfaces;
using SIGO.Objects.Models;
using SIGO.Services.Entities;
using SIGO.Services.Interfaces;
using Xunit;

namespace SIGO.Tests.Services;

public sealed class VeiculoRemovalServiceTests
{
    [Fact]
    public async Task RemoveForOficina_DeveRemoverSomenteVeiculoVisivelParaOficina()
    {
        var veiculo = new Veiculo { Id = 4, ClienteId = 5 };
        var repository = new Mock<IVeiculoRepository>();
        repository
            .Setup(item => item.GetByIdForOficina(4, 2))
            .ReturnsAsync(veiculo);
        var service = CreateService(repository);

        await service.RemoveForOficina(4, 2);

        repository.Verify(item => item.Remove(veiculo), Times.Once);
        repository.Verify(item => item.GetById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RemoveForOficina_DeveFalhar_QuandoVeiculoNaoPertenceAoEscopoDaOficina()
    {
        var repository = new Mock<IVeiculoRepository>();
        repository
            .Setup(item => item.GetByIdForOficina(4, 2))
            .ReturnsAsync((Veiculo?)null);
        var service = CreateService(repository);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.RemoveForOficina(4, 2));

        repository.Verify(item => item.Remove(It.IsAny<Veiculo>()), Times.Never);
    }

    private static VeiculoService CreateService(Mock<IVeiculoRepository> repository) =>
        new(
            repository.Object,
            Mock.Of<IMapper>(),
            Mock.Of<IClienteRepository>(),
            Mock.Of<IVeiculoImagemStorageService>());
}
