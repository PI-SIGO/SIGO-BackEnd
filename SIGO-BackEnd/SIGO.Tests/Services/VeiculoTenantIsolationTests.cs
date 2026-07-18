using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SIGO.Data.Interfaces;
using SIGO.Objects.Dtos.Mappings;
using SIGO.Objects.Models;
using SIGO.Services.Entities;
using SIGO.Services.Interfaces;
using Xunit;

namespace SIGO.Tests.Services
{
    public class VeiculoTenantIsolationTests
    {
        [Theory]
        [InlineData(10, 101, 201)]
        [InlineData(20, 102, 202)]
        public async Task GetByOficina_DeveExporSomenteHistoricosDaOficinaAutenticada(
            int oficinaId,
            int expectedOrderId,
            int expectedRecordId)
        {
            var repository = new Mock<IVeiculoRepository>();
            repository
                .Setup(item => item.GetByOficina(oficinaId))
                .ReturnsAsync(new[] { CreateVehicleWithHistoriesFromTwoWorkshops() });

            var service = CreateService(repository.Object);

            var vehicle = Assert.Single(await service.GetByOficina(oficinaId));

            Assert.Equal(expectedOrderId, Assert.Single(vehicle.Pedidos).Id);
            Assert.Equal(expectedRecordId, Assert.Single(vehicle.RegistroServicos).Id);
            Assert.All(vehicle.Pedidos, order => Assert.Equal(oficinaId, order.idOficina));
            Assert.All(
                vehicle.RegistroServicos,
                record => Assert.Equal(oficinaId, record.OficinaId));
        }

        [Theory]
        [InlineData(10, 101, 201)]
        [InlineData(20, 102, 202)]
        public async Task GetByIdForOficina_DeveExporSomenteHistoricosDaOficinaAutenticada(
            int oficinaId,
            int expectedOrderId,
            int expectedRecordId)
        {
            var repository = new Mock<IVeiculoRepository>();
            repository
                .Setup(item => item.GetByIdForOficina(1, oficinaId))
                .ReturnsAsync(CreateVehicleWithHistoriesFromTwoWorkshops());

            var service = CreateService(repository.Object);

            var vehicle = await service.GetByIdForOficina(1, oficinaId);

            Assert.NotNull(vehicle);
            Assert.Equal(expectedOrderId, Assert.Single(vehicle.Pedidos).Id);
            Assert.Equal(expectedRecordId, Assert.Single(vehicle.RegistroServicos).Id);
        }

        [Fact]
        public async Task GetByCliente_DeveManterHistoricosDasDuasOficinasDoProprioVeiculo()
        {
            var repository = new Mock<IVeiculoRepository>();
            repository
                .Setup(item => item.GetByCliente(7))
                .ReturnsAsync(new[] { CreateVehicleWithHistoriesFromTwoWorkshops() });

            var service = CreateService(repository.Object);

            var vehicle = Assert.Single(await service.GetByCliente(7));

            Assert.Equal(new[] { 101, 102 }, vehicle.Pedidos.Select(order => order.Id).Order());
            Assert.Equal(new[] { 201, 202 }, vehicle.RegistroServicos.Select(record => record.Id).Order());
        }

        private static VeiculoService CreateService(IVeiculoRepository repository)
        {
            var mapperConfiguration = new MapperConfiguration(
                configuration => configuration.AddProfile<MappingProfile>(),
                NullLoggerFactory.Instance);

            return new VeiculoService(
                repository,
                mapperConfiguration.CreateMapper(),
                Mock.Of<IClienteRepository>(),
                Mock.Of<IVeiculoImagemStorageService>());
        }

        private static Veiculo CreateVehicleWithHistoriesFromTwoWorkshops()
        {
            return new Veiculo
            {
                Id = 1,
                ClienteId = 7,
                Pedidos = new List<Pedido>
                {
                    new() { Id = 101, idOficina = 10, idCliente = 7, idVeiculo = 1 },
                    new() { Id = 102, idOficina = 20, idCliente = 7, idVeiculo = 1 }
                },
                RegistroServicos = new List<RegistroServico>
                {
                    new()
                    {
                        Id = 201,
                        VeiculoId = 1,
                        OficinaId = 10,
                        ServicoId = 301,
                        Servico = new Servico { Id = 301, IdOficina = 10 }
                    },
                    new()
                    {
                        Id = 202,
                        VeiculoId = 1,
                        OficinaId = 20,
                        ServicoId = 302,
                        Servico = new Servico { Id = 302, IdOficina = 20 }
                    }
                }
            };
        }
    }
}
