using AutoMapper;
using Moq;
using SIGO.Data.Interfaces;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;
using SIGO.Services.Entities;
using Xunit;

namespace SIGO.Tests.Services;

public class MarcaServiceTests
{
    [Fact]
    public async Task CreateMarca_DeveRetornarIdGeradoPeloBanco()
    {
        var repository = new Mock<IMarcaRepository>();
        var mapper = new Mock<IMapper>();
        var input = new MarcaDTO { Id = 55, Nome = "Fiat" };
        mapper.Setup(m => m.Map<Marca>(input)).Returns(() => new Marca { Nome = input.Nome });
        repository
            .Setup(r => r.Add(It.IsAny<Marca>()))
            .Callback<Marca>(entity => entity.Id = 8)
            .Returns(Task.CompletedTask);
        mapper.Setup(m => m.Map<MarcaDTO>(It.IsAny<Marca>()))
            .Returns<Marca>(entity => new MarcaDTO { Id = entity.Id, Nome = entity.Nome });
        var service = new MarcaService(repository.Object, mapper.Object);

        var result = await service.CreateMarca(input);

        Assert.Equal(8, result.Id);
        Assert.Equal(0, input.Id);
    }

    [Fact]
    public async Task UpdateMarca_DeveLancarNotFound_QuandoMarcaNaoExiste()
    {
        var repository = new Mock<IMarcaRepository>();
        repository.Setup(r => r.GetById(99)).ReturnsAsync((Marca?)null);
        var service = new MarcaService(repository.Object, Mock.Of<IMapper>());

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.UpdateMarca(new MarcaDTO(), 99));

        repository.Verify(r => r.Update(It.IsAny<Marca>()), Times.Never);
    }

    [Fact]
    public async Task Remove_DeveLancarNotFound_QuandoMarcaNaoExiste()
    {
        var repository = new Mock<IMarcaRepository>();
        repository.Setup(r => r.GetById(99)).ReturnsAsync((Marca?)null);
        var service = new MarcaService(repository.Object, Mock.Of<IMapper>());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.Remove(99));

        repository.Verify(r => r.Remove(It.IsAny<Marca>()), Times.Never);
    }
}
