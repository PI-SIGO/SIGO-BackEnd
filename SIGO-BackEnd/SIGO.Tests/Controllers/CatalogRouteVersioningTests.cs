using Microsoft.AspNetCore.Mvc;
using SIGO.Controllers;
using Xunit;

namespace SIGO.Tests.Controllers;

public class CatalogRouteVersioningTests
{
    [Theory]
    [InlineData(typeof(VeiculoController), "api/v1/veiculos")]
    [InlineData(typeof(TelefoneController), "api/v1/telefones")]
    [InlineData(typeof(MarcaController), "api/v1/marcas")]
    [InlineData(typeof(CepController), "api/v1/ceps")]
    public void Controller_DeveExporSomenteRotaOficial(
        Type controllerType,
        string officialRoute)
    {
        var routes = controllerType
            .GetCustomAttributes(typeof(RouteAttribute), inherit: true)
            .Cast<RouteAttribute>()
            .Select(attribute => attribute.Template)
            .ToArray();

        Assert.Equal(officialRoute, Assert.Single(routes));
    }

    [Fact]
    public void Report_DeveExporSomenteRotaOficial()
    {
        var controllerRoute = Assert.Single(typeof(ReportController)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: true)
            .Cast<RouteAttribute>());
        Assert.Equal("api/v1/relatorios", controllerRoute.Template);

        var action = typeof(ReportController).GetMethod(nameof(ReportController.GetVehicleHistoryPdf));
        Assert.NotNull(action);
        var routes = action!
            .GetCustomAttributes(typeof(HttpGetAttribute), inherit: true)
            .Cast<HttpGetAttribute>()
            .Select(attribute => attribute.Template)
            .ToArray();

        Assert.Equal("veiculos/{veiculoId:int}", Assert.Single(routes));
    }
}
