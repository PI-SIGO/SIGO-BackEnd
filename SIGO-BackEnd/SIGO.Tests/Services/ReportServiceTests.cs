using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Moq;
using QuestPDF.Infrastructure;
using SIGO.Data;
using SIGO.Data.Interfaces;
using SIGO.Objects.Enums;
using SIGO.Objects.Models;
using SIGO.Security;
using SIGO.Services.Entities;
using Xunit;

namespace SIGO.Tests.Services;

public sealed class ReportServiceTests
{
    [Fact]
    public async Task GetVehicleHistoryAsync_DeveResolverNomesEItensDoPedido()
    {
        await using var context = CreateContext();
        await SeedVehicleAsync(context);
        var registroRepository = CreateRegistroRepository(Array.Empty<RegistroServico>());
        var pedidoRepository = CreatePedidoRepository(new[] { CreateOrder() });
        var service = CreateService(context, registroRepository, pedidoRepository);

        var history = await service.GetVehicleHistoryAsync(10, cancellationToken: CancellationToken.None);

        Assert.Equal(5, history.Cliente.Id);
        Assert.Equal("Cliente Teste", history.Cliente.Nome);
        Assert.Equal("ABC1D23", history.Veiculo.Placa);
        var item = Assert.Single(history.Eventos);
        Assert.Equal("Pedido", item.Origem);
        Assert.Equal(30, item.PedidoId);
        Assert.Equal("Oficina Central", item.OficinaNome);
        Assert.Equal(9, item.FuncionarioId);
        Assert.Equal("Mecanico Teste", item.FuncionarioNome);
        var servico = Assert.Single(item.Servicos);
        Assert.Equal("Troca de oleo", servico.Nome);
        Assert.Equal(100m, servico.Valor);
        var peca = Assert.Single(item.Pecas);
        Assert.Equal("Filtro de oleo", peca.Nome);
        Assert.Equal(50m, peca.ValorUnitario);
    }

    [Fact]
    public async Task GenerateVehicleHistoryExcelAsync_DeveCriarResumoEHistorico()
    {
        await using var context = CreateContext();
        await SeedVehicleAsync(context);
        var service = CreateService(
            context,
            CreateRegistroRepository(Array.Empty<RegistroServico>()),
            CreatePedidoRepository(new[] { CreateOrder() }));

        var bytes = await service.GenerateVehicleHistoryExcelAsync(
            10,
            cancellationToken: CancellationToken.None);

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var summarySheet = workbook.Worksheet("Resumo");
        Assert.DoesNotContain(
            summarySheet.Column(1).CellsUsed().Select(cell => cell.GetString()),
            value => value.Contains("ID", StringComparison.OrdinalIgnoreCase));
        var historySheet = workbook.Worksheet("Historico");
        Assert.DoesNotContain(
            historySheet.Row(1).CellsUsed().Select(cell => cell.GetString()),
            value => value.Contains("ID", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("Oficina Central", historySheet.Cell(2, 4).GetString());
        Assert.Equal("Mecanico Teste", historySheet.Cell(2, 5).GetString());
        Assert.Contains("Troca de oleo", historySheet.Cell(2, 6).GetString());
        Assert.Contains("Filtro de oleo", historySheet.Cell(2, 7).GetString());
    }

    [Fact]
    public async Task GenerateVehicleHistoryPdfAsync_DeveCriarPdfComHistorico()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        await using var context = CreateContext();
        await SeedVehicleAsync(context);
        var service = CreateService(
            context,
            CreateRegistroRepository(Array.Empty<RegistroServico>()),
            CreatePedidoRepository(new[] { CreateOrder() }));

        var bytes = await service.GenerateVehicleHistoryPdfAsync(
            10,
            cancellationToken: CancellationToken.None);

        Assert.True(bytes.Length > 4);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task SeedVehicleAsync(AppDbContext context)
    {
        context.Clientes.Add(new Cliente
        {
            Id = 5,
            Nome = "Cliente Teste",
            Email = "cliente@example.com",
            Cpf_Cnpj = "52998224725",
            Situacao = Situacao.ATIVO
        });
        context.Veiculos.Add(new Veiculo
        {
            Id = 10,
            ClienteId = 5,
            NomeVeiculo = "Onix",
            TipoVeiculo = "Carro",
            PlacaVeiculo = "ABC1D23",
            ChassiVeiculo = "12345678901234567",
            AnoFab = 2024,
            Quilometragem = 1000,
            Combustivel = "Flex",
            Seguro = "Sim",
            Cor = "Prata",
            Status = Status.Pendente
        });
        await context.SaveChangesAsync();
    }

    private static Pedido CreateOrder() => new()
    {
        Id = 30,
        idCliente = 5,
        idVeiculo = 10,
        idOficina = 7,
        Oficina = new Oficina { Id = 7, Nome = "Oficina Central" },
        idFuncionario = 9,
        Funcionario = new Funcionario { Id = 9, Nome = "Mecanico Teste" },
        DataInicio = new DateOnly(2026, 8, 1),
        DataFim = new DateOnly(2026, 8, 2),
        ValorTotal = 140,
        DescontoTotalReais = 10,
        Observacao = "Revisao completa",
        Pedido_Servicos = new List<Pedido_Servico>
        {
            new()
            {
                IdServico = 11,
                QuantVezes = 1,
                ValorUnitario = 100,
                Servico = new Servico
                {
                    Id = 11,
                    Nome = "Troca de oleo",
                    Descricao = "Substituicao do lubrificante",
                    Valor = 999
                }
            }
        },
        Pedido_Pecas = new List<Pedido_Peca>
        {
            new()
            {
                IdPeca = 12,
                Quantidade = 1,
                ValorUnitario = 50,
                DataInstalacao = new DateOnly(2026, 8, 1),
                Estado = "Nova",
                Observacao = "Substituida durante a revisao",
                Peca = new Peca
                {
                    Id = 12,
                    Nome = "Filtro de oleo",
                    Valor = 999
                }
            }
        }
    };

    private static Mock<IRegistroServicoRepository> CreateRegistroRepository(
        IEnumerable<RegistroServico> records)
    {
        var repository = new Mock<IRegistroServicoRepository>();
        repository
            .Setup(item => item.GetByVeiculoAsync(
                10,
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(records);
        return repository;
    }

    private static Mock<IPedidoRepository> CreatePedidoRepository(IEnumerable<Pedido> orders)
    {
        var repository = new Mock<IPedidoRepository>();
        repository
            .Setup(item => item.GetByVeiculoWithDetailsAsync(
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);
        return repository;
    }

    private static ReportService CreateService(
        AppDbContext context,
        Mock<IRegistroServicoRepository> registroRepository,
        Mock<IPedidoRepository> pedidoRepository)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser
            .Setup(item => item.IsInRole(SystemRoles.Admin))
            .Returns(true);

        return new ReportService(
            registroRepository.Object,
            pedidoRepository.Object,
            context,
            currentUser.Object);
    }
}
