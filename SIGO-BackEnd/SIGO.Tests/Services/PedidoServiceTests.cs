using AutoMapper;
using Moq;
using SIGO.Data.Interfaces;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Enums;
using SIGO.Objects.Models;
using SIGO.Services.Entities;
using SIGO.Validation;
using Xunit;

namespace SIGO.Tests.Services
{
    public class PedidoServiceTests
    {
        private readonly Mock<IPedidoRepository> _orders = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<IClienteRepository> _clients = new();
        private readonly Mock<IFuncionarioRepository> _employees = new();
        private readonly Mock<IVeiculoRepository> _vehicles = new();
        private readonly Mock<IPecaRepository> _pieces = new();
        private readonly Mock<IServicoRepository> _services = new();

        public PedidoServiceTests()
        {
            _clients.Setup(repository => repository.ExistsInOficina(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(true);
            _employees.Setup(repository => repository.ExistsInOficina(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(true);
            _vehicles.Setup(repository => repository.GetById(It.IsAny<int>()))
                .ReturnsAsync((int id) => new Veiculo { Id = id, ClienteId = 10 });
            _pieces.Setup(repository => repository.GetByIdsAsync(
                    It.IsAny<IReadOnlyCollection<int>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyCollection<int> ids, CancellationToken _) =>
                    ids.Select(id => new Peca { Id = id, IdOficina = 3 }).ToArray());
            _services.Setup(repository => repository.GetByIdsAsync(
                    It.IsAny<IReadOnlyCollection<int>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyCollection<int> ids, CancellationToken _) =>
                    ids.Select(id => new Servico { Id = id, IdOficina = 3 }).ToArray());
        }

        [Fact]
        public async Task Create_AdminValidaTenantEDevolveIdPersistido()
        {
            var request = CreateRequest();
            var entity = new Pedido();
            _mapper.Setup(mapper => mapper.Map<Pedido>(request)).Returns(entity);
            _orders.Setup(repository => repository.Add(entity))
                .Callback(() => entity.Id = 77)
                .Returns(Task.CompletedTask);

            await CreateService().Create(request);

            Assert.Equal(77, request.Id);
            _orders.Verify(repository => repository.Add(entity), Times.Once);
            _clients.Verify(repository => repository.ExistsInOficina(10, 3), Times.Once);
        }

        [Fact]
        public async Task Create_DevePreservarTotalInformadoECalcularDescontosComPrecosDoCatalogo()
        {
            var request = CreateRequest();
            request.ValorTotal = 9999m;
            request.DescontoTotalReais = 9999m;
            request.DescontoServicoPorcentagem = 10m;
            request.descontoPecaReais = 5m;
            request.DescontoPorcentagem = 10m;
            request.Pedido_Pecas.Add(new Pedido_PecaDTO
            {
                IdPeca = 8,
                Quantidade = 2,
                ValorUnitario = 15m,
                Estado = "Nova"
            });
            request.Pedido_Servicos.Add(new Pedido_ServicoDTO
            {
                IdServico = 6,
                QuantVezes = 3
            });

            _pieces.Setup(repository => repository.GetByIdsAsync(
                    It.IsAny<IReadOnlyCollection<int>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { new Peca { Id = 8, IdOficina = 3, Valor = 40m } });
            _services.Setup(repository => repository.GetByIdsAsync(
                    It.IsAny<IReadOnlyCollection<int>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { new Servico { Id = 6, IdOficina = 3, Valor = 100m } });

            var entity = new Pedido();
            _mapper.Setup(mapper => mapper.Map<Pedido>(request)).Returns(entity);
            _orders.Setup(repository => repository.Add(entity)).Returns(Task.CompletedTask);

            await CreateService().Create(request);

            Assert.Equal(9999m, request.ValorTotal);
            Assert.Equal(64.50m, request.DescontoTotalReais);
            Assert.Equal(9999m, request.ValorLiquido);
            Assert.Equal(330m, request.ValorBruto);
            Assert.Equal(30m, request.SubtotalPecas);
            Assert.Equal(300m, request.SubtotalServicos);
            Assert.Equal(15m, request.Pedido_Pecas.Single().ValorUnitario);
            Assert.Equal(100m, request.Pedido_Servicos.Single().ValorUnitario);
        }

        [Fact]
        public async Task Create_DeveRejeitarValorTotalNegativo()
        {
            var request = CreateRequest();
            request.ValorTotal = -1m;

            var exception = await Assert.ThrowsAsync<BusinessValidationException>(
                () => CreateService().Create(request));

            Assert.Contains(exception.Errors, error =>
                error.Field == nameof(PedidoDTO.ValorTotal));
            _orders.Verify(repository => repository.Add(It.IsAny<Pedido>()), Times.Never);
        }

        [Fact]
        public async Task Create_DeveRejeitarValorUnitarioNegativoNaPeca()
        {
            var request = CreateRequest();
            request.Pedido_Pecas.Add(new Pedido_PecaDTO
            {
                IdPeca = 8,
                Quantidade = 1,
                ValorUnitario = -1m,
                Estado = "Nova"
            });

            var exception = await Assert.ThrowsAsync<BusinessValidationException>(
                () => CreateService().Create(request));

            Assert.Contains(exception.Errors, error =>
                error.Field == nameof(PedidoDTO.Pedido_Pecas));
            _orders.Verify(repository => repository.Add(It.IsAny<Pedido>()), Times.Never);
        }

        [Fact]
        public async Task Create_DeveRejeitarDescontoEmReaisEPorcentagemNoMesmoNivel()
        {
            var request = CreateRequest();
            request.DescontoReais = 10m;
            request.DescontoPorcentagem = 5m;

            var exception = await Assert.ThrowsAsync<BusinessValidationException>(
                () => CreateService().Create(request));

            Assert.Contains(exception.Errors, error =>
                error.Field == nameof(PedidoDTO.DescontoReais) &&
                error.Message.Contains("nunca os dois"));
            _orders.Verify(repository => repository.Add(It.IsAny<Pedido>()), Times.Never);
        }

        [Fact]
        public async Task Create_DeveRejeitarDescontoFixoMaiorQueSubtotal()
        {
            var request = CreateRequest();
            request.DescontoServicoReais = 101m;
            request.Pedido_Servicos.Add(new Pedido_ServicoDTO
            {
                IdServico = 6,
                QuantVezes = 1
            });
            _services.Setup(repository => repository.GetByIdsAsync(
                    It.IsAny<IReadOnlyCollection<int>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { new Servico { Id = 6, IdOficina = 3, Valor = 100m } });

            var exception = await Assert.ThrowsAsync<BusinessValidationException>(
                () => CreateService().Create(request));

            Assert.Contains(exception.Errors, error =>
                error.Field == nameof(PedidoDTO.DescontoServicoReais) &&
                error.Message.Contains("nao pode superar"));
            _orders.Verify(repository => repository.Add(It.IsAny<Pedido>()), Times.Never);
        }

        [Fact]
        public async Task Create_AdminRejeitaPecaDeOutraOficina()
        {
            var request = CreateRequest();
            request.Pedido_Pecas.Add(new Pedido_PecaDTO
            {
                IdPeca = 9,
                Quantidade = 1,
                Estado = "Nova"
            });
            _pieces.Setup(repository => repository.GetByIdsAsync(
                    It.IsAny<IReadOnlyCollection<int>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { new Peca { Id = 9, IdOficina = 99 } });

            var exception = await Assert.ThrowsAsync<BusinessValidationException>(
                () => CreateService().Create(request));

            Assert.Contains(exception.Errors, error =>
                error.Field == nameof(PedidoDTO.Pedido_Pecas) &&
                error.Message.Contains("nao pertence a oficina"));
            _orders.Verify(repository => repository.Add(It.IsAny<Pedido>()), Times.Never);
        }

        [Fact]
        public async Task Create_AdminRejeitaServicoDeOutraOficina()
        {
            var request = CreateRequest();
            request.Pedido_Servicos.Add(new Pedido_ServicoDTO { IdServico = 4, QuantVezes = 1 });
            _services.Setup(repository => repository.GetByIdsAsync(
                    It.IsAny<IReadOnlyCollection<int>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { new Servico { Id = 4, IdOficina = 22 } });

            var exception = await Assert.ThrowsAsync<BusinessValidationException>(
                () => CreateService().Create(request));

            Assert.Contains(exception.Errors, error =>
                error.Field == nameof(PedidoDTO.Pedido_Servicos) &&
                error.Message.Contains("nao pertence a oficina"));
        }

        [Fact]
        public async Task Update_SincronizaPecasEServicosEmUmaOperacaoDeRepositorio()
        {
            var existing = new Pedido
            {
                Id = 15,
                idOficina = 3,
                idCliente = 10,
                idFuncionario = 5,
                idVeiculo = 20,
                Status = Status.EmAndamento,
                Pedido_Pecas = new List<Pedido_Peca>(),
                Pedido_Servicos = new List<Pedido_Servico>()
            };
            var request = CreateRequest();
            request.ValorTotal = 425m;
            request.Status = Status.Concluido;
            request.Pedido_Pecas.Add(new Pedido_PecaDTO
            {
                IdPeca = 8,
                Quantidade = 2,
                ValorUnitario = 17.50m,
                Estado = "Nova",
                DataInstalacao = DateOnly.FromDateTime(DateTime.Today)
            });
            request.Pedido_Servicos.Add(new Pedido_ServicoDTO { IdServico = 6, QuantVezes = 2 });
            _pieces.Setup(repository => repository.GetByIdsAsync(
                    It.IsAny<IReadOnlyCollection<int>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { new Peca { Id = 8, IdOficina = 3, Valor = 25m } });
            _services.Setup(repository => repository.GetByIdsAsync(
                    It.IsAny<IReadOnlyCollection<int>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { new Servico { Id = 6, IdOficina = 3, Valor = 60m } });
            _orders.Setup(repository => repository.GetByIdWithDetails(15)).ReturnsAsync(existing);
            _orders.Setup(repository => repository.SaveWithDetailsAsync(
                    existing,
                    It.IsAny<IReadOnlyCollection<Pedido_Peca>>(),
                    It.IsAny<IReadOnlyCollection<Pedido_Servico>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            await CreateService().Update(request, 15);

            Assert.Equal(15, request.Id);
            _orders.Verify(repository => repository.SaveWithDetailsAsync(
                existing,
                It.Is<IReadOnlyCollection<Pedido_Peca>>(items =>
                    items.Count == 1 &&
                    items.Single().IdPeca == 8 &&
                    items.Single().ValorUnitario == 17.50m),
                It.Is<IReadOnlyCollection<Pedido_Servico>>(items =>
                    items.Count == 1 &&
                    items.Single().IdServico == 6 &&
                    items.Single().ValorUnitario == 60m),
                It.IsAny<CancellationToken>()), Times.Once);
            _orders.Verify(repository => repository.SaveChanges(), Times.Never);
            Assert.Equal(425m, request.ValorTotal);
            Assert.Equal(425m, existing.ValorTotal);
            Assert.Equal(425m, request.ValorLiquido);
            Assert.Equal(Status.EmAndamento, request.Status);
        }

        [Fact]
        public async Task CreateForOficina_RejeitaVeiculoDeOutroCliente()
        {
            var request = CreateRequest();
            _vehicles.Setup(repository => repository.GetById(20))
                .ReturnsAsync(new Veiculo { Id = 20, ClienteId = 999 });

            var exception = await Assert.ThrowsAsync<BusinessValidationException>(
                () => CreateService().CreateForOficina(request, 3));

            Assert.Contains(exception.Errors, error =>
                error.Field == nameof(PedidoDTO.idVeiculo) &&
                error.Message == "Veiculo nao pertence ao cliente informado.");
        }

        [Fact]
        public async Task GetById_CarregaDetalhes()
        {
            var entity = new Pedido { Id = 3 };
            var dto = new PedidoDTO { Id = 3 };
            _orders.Setup(repository => repository.GetByIdWithDetails(3)).ReturnsAsync(entity);
            _mapper.Setup(mapper => mapper.Map<PedidoDTO>(entity)).Returns(dto);

            var result = await CreateService().GetById(3);

            Assert.Same(dto, result);
            _orders.Verify(repository => repository.GetByIdWithDetails(3), Times.Once);
        }

        [Fact]
        public async Task Create_DeveForcarStatusPendenteNoBackend()
        {
            var request = CreateRequest();
            request.Status = Status.Concluido;
            var entity = new Pedido();
            _mapper.Setup(mapper => mapper.Map<Pedido>(request))
                .Callback(() => entity.Status = request.Status)
                .Returns(entity);
            _orders.Setup(repository => repository.Add(entity)).Returns(Task.CompletedTask);

            await CreateService().Create(request);

            Assert.Equal(Status.Pendente, request.Status);
            Assert.Equal(Status.Pendente, entity.Status);
        }

        [Fact]
        public async Task UpdateStatus_DevePersistirStatusDoPedido()
        {
            var entity = new Pedido { Id = 3, Status = Status.Pendente };
            var dto = new PedidoDTO { Id = 3, Status = Status.EmAndamento };
            _orders.Setup(repository => repository.GetByIdWithDetails(3)).ReturnsAsync(entity);
            _orders.Setup(repository => repository.SaveChanges(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            _mapper.Setup(mapper => mapper.Map<PedidoDTO>(entity)).Returns(dto);

            var result = await CreateService().UpdateStatus(
                3,
                Status.EmAndamento,
                CancellationToken.None);

            Assert.Equal(Status.EmAndamento, entity.Status);
            Assert.Same(dto, result);
            _orders.Verify(repository => repository.SaveChanges(
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateStatusForOficina_DeveBuscarPedidoNoTenant()
        {
            var entity = new Pedido { Id = 3, idOficina = 7, Status = Status.Pendente };
            _orders.Setup(repository => repository.GetByIdForOficina(3, 7)).ReturnsAsync(entity);
            _orders.Setup(repository => repository.SaveChanges(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            _mapper.Setup(mapper => mapper.Map<PedidoDTO>(entity))
                .Returns(new PedidoDTO { Id = 3, Status = Status.Concluido });

            await CreateService().UpdateStatusForOficina(
                3,
                Status.Concluido,
                7,
                CancellationToken.None);

            Assert.Equal(Status.Concluido, entity.Status);
            _orders.Verify(repository => repository.GetByIdForOficina(3, 7), Times.Once);
            _orders.Verify(repository => repository.GetByIdWithDetails(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task UpdateStatus_DeveRejeitarValorForaDoEnum()
        {
            var invalidStatus = (Status)999;

            var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
                CreateService().UpdateStatus(3, invalidStatus, CancellationToken.None));

            Assert.Contains(exception.Errors, error => error.Field == nameof(PedidoDTO.Status));
            _orders.Verify(repository => repository.SaveChanges(
                It.IsAny<CancellationToken>()), Times.Never);
        }

        private PedidoService CreateService() => new(
            _orders.Object,
            _mapper.Object,
            _clients.Object,
            _employees.Object,
            _vehicles.Object,
            _pieces.Object,
            _services.Object);

        private static PedidoDTO CreateRequest() => new()
        {
            idCliente = 10,
            idFuncionario = 5,
            idOficina = 3,
            idVeiculo = 20,
            Observacao = "Revisao",
            DataInicio = DateOnly.FromDateTime(DateTime.Today),
            DataFim = DateOnly.FromDateTime(DateTime.Today),
            Pedido_Pecas = new List<Pedido_PecaDTO>(),
            Pedido_Servicos = new List<Pedido_ServicoDTO>()
        };
    }
}
