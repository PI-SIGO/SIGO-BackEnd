using AutoMapper;
using Moq;
using SIGO.Data.Interfaces;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;
using SIGO.Services.Entities;
using Xunit;

namespace SIGO.Tests.Services;

public class TelefoneServiceTests
{
    [Fact]
    public async Task CreateTelefone_DeveRetornarIdGeradoPeloBanco()
    {
        var repository = new Mock<ITelefoneRepository>();
        var mapper = new Mock<IMapper>();
        var input = new TelefoneDTO { Id = 77, DDD = 47, Numero = "999999999", ClienteId = 3 };
        mapper.Setup(m => m.Map<Telefone>(input)).Returns(() => new Telefone
        {
            DDD = input.DDD,
            Numero = input.Numero,
            ClienteId = input.ClienteId
        });
        repository
            .Setup(r => r.Add(It.IsAny<Telefone>()))
            .Callback<Telefone>(entity => entity.Id = 12)
            .Returns(Task.CompletedTask);
        mapper.Setup(m => m.Map<TelefoneDTO>(It.IsAny<Telefone>()))
            .Returns<Telefone>(entity => new TelefoneDTO
            {
                Id = entity.Id,
                DDD = entity.DDD,
                Numero = entity.Numero,
                ClienteId = entity.ClienteId
            });
        var service = new TelefoneService(repository.Object, mapper.Object);

        var result = await service.CreateTelefone(input);

        Assert.Equal(12, result.Id);
        Assert.Equal(0, input.Id);
    }

    [Fact]
    public async Task UpdateTelefone_DeveLancarNotFound_QuandoTelefoneNaoExiste()
    {
        var repository = new Mock<ITelefoneRepository>();
        repository.Setup(r => r.GetById(99)).ReturnsAsync((Telefone?)null);
        var service = new TelefoneService(repository.Object, Mock.Of<IMapper>());

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.UpdateTelefone(new TelefoneDTO(), 99));

        repository.Verify(r => r.SaveChanges(), Times.Never);
    }
}
