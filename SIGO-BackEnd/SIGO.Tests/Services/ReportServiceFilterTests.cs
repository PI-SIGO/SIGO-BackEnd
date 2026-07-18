using SIGO.Objects.Models;
using SIGO.Services.Entities;
using SIGO.Validation;
using Xunit;

namespace SIGO.Tests.Services;

public class ReportServiceFilterTests
{
    [Fact]
    public void FilterPedidos_DeveAplicarPeriodoETipoDeServico()
    {
        var pedidos = new[]
        {
            CriarPedido(1, new DateOnly(2026, 1, 10), "Troca de óleo"),
            CriarPedido(2, new DateOnly(2026, 1, 20), "Alinhamento"),
            CriarPedido(3, new DateOnly(2025, 12, 20), "Troca de óleo")
        };

        var result = ReportService.FilterPedidos(
                pedidos,
                new DateTime(2026, 1, 1),
                new DateTime(2026, 1, 31),
                "ÓLEO")
            .ToArray();

        Assert.Equal(1, Assert.Single(result).Id);
    }

    [Fact]
    public void FilterPedidos_DeveRejeitarPeriodoInvertido()
    {
        Assert.Throws<BusinessValidationException>(() => ReportService.FilterPedidos(
                Array.Empty<Pedido>(),
                new DateTime(2026, 2, 1),
                new DateTime(2026, 1, 1),
                null)
            .ToArray());
    }

    private static Pedido CriarPedido(int id, DateOnly dataInicio, string servico) => new()
    {
        Id = id,
        DataInicio = dataInicio,
        Pedido_Servicos = new List<Pedido_Servico>
        {
            new()
            {
                Servico = new Servico { Nome = servico }
            }
        }
    };
}
