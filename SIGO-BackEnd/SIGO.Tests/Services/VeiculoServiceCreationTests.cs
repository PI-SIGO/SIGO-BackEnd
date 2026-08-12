using AutoMapper;
using Moq;
using SIGO.Data.Interfaces;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Enums;
using SIGO.Objects.Models;
using SIGO.Services.Entities;
using SIGO.Services.Interfaces;
using SIGO.Validation;
using Xunit;

namespace SIGO.Tests.Services;

public class VeiculoServiceCreationTests
{
    [Fact]
    public async Task CreateVeiculo_DeveDefinirClienteEStatusNoBackend()
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

        var result = await service.CreateVeiculo(input, 4);

        Assert.Equal(15, result.Id);
        Assert.Equal(4, result.ClienteId);
        Assert.NotNull(entityPersisted);
        Assert.Equal(4, entityPersisted.ClienteId);
        Assert.Equal(SIGO.Objects.Enums.Status.Pendente, entityPersisted.Status);
    }

    [Fact]
    public async Task UpdateStatusForOficina_DevePersistirSomenteVeiculoDoTenant()
    {
        var repository = new Mock<IVeiculoRepository>();
        var mapper = new Mock<IMapper>();
        var entity = new Veiculo
        {
            Id = 15,
            ClienteId = 4,
            Status = Status.Pendente
        };
        repository.Setup(item => item.GetByIdForOficina(15, 7)).ReturnsAsync(entity);
        repository.Setup(item => item.SaveChanges(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        mapper.Setup(item => item.Map<VeiculoDTO>(entity)).Returns(new VeiculoDTO
        {
            Id = 15,
            ClienteId = 4,
            Status = Status.EmAndamento
        });
        var service = new VeiculoService(
            repository.Object,
            mapper.Object,
            Mock.Of<IClienteRepository>(),
            Mock.Of<IVeiculoImagemStorageService>());

        var result = await service.UpdateStatusForOficina(
            15,
            Status.EmAndamento,
            7,
            CancellationToken.None);

        Assert.Equal(Status.EmAndamento, entity.Status);
        Assert.Equal(Status.EmAndamento, result.Status);
        repository.Verify(item => item.GetByIdForOficina(15, 7), Times.Once);
        repository.Verify(item => item.GetById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatus_DeveRejeitarValorForaDoEnum()
    {
        var repository = new Mock<IVeiculoRepository>();
        var service = new VeiculoService(
            repository.Object,
            Mock.Of<IMapper>(),
            Mock.Of<IClienteRepository>(),
            Mock.Of<IVeiculoImagemStorageService>());

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.UpdateStatus(15, (Status)999, CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.Field == nameof(VeiculoDTO.Status));
        repository.Verify(item => item.SaveChanges(
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
