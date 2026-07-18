using AutoMapper;
using Moq;
using SIGO.Data.Interfaces;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;
using SIGO.Services.Entities;
using SIGO.Services.Interfaces;
using Xunit;

namespace SIGO.Tests.Services;

public class VeiculoServiceCreationTests
{
    [Fact]
    public async Task CreateForCliente_DeveDefinirClienteEStatusNoBackend()
    {
        var repository = new Mock<IVeiculoRepository>();
        var mapper = new Mock<IMapper>();
        var clienteRepository = new Mock<IClienteRepository>();
        var input = new VeiculoRequestDTO { NomeVeiculo = "Onix" };
        clienteRepository.Setup(r => r.GetById(4)).ReturnsAsync(new Cliente { Id = 4 });
        Veiculo? entityPersisted = null;
        repository
            .Setup(r => r.Add(It.IsAny<Veiculo>()))
            .Callback<Veiculo>(entity =>
            {
                entity.Id = 15;
                entityPersisted = entity;
            })
            .Returns(Task.CompletedTask);
        mapper.Setup(m => m.Map<VeiculoDTO>(It.IsAny<Veiculo>()))
            .Returns<Veiculo>(entity => new VeiculoDTO
            {
                Id = entity.Id,
                ClienteId = entity.ClienteId,
                NomeVeiculo = entity.NomeVeiculo
            });
        var service = new VeiculoService(
            repository.Object,
            mapper.Object,
            clienteRepository.Object,
            Mock.Of<IVeiculoImagemStorageService>());

        var result = await service.CreateForCliente(input, 4);

        Assert.Equal(15, result.Id);
        Assert.Equal(4, result.ClienteId);
        Assert.NotNull(entityPersisted);
        Assert.Equal(4, entityPersisted.ClienteId);
        Assert.Equal(SIGO.Objects.Enums.Status.Pendente, entityPersisted.Status);
    }
}
