using Microsoft.AspNetCore.Mvc;
using SIGO.Controllers;
using Xunit;

namespace SIGO.Tests.Controllers
{
    public class OperationalRouteContractTests
    {
        [Theory]
        [MemberData(nameof(ControllersAndRoutes))]
        public void Controller_ExpoeSomenteRotaV1(
            Type controllerType,
            string officialRoute)
        {
            var routes = controllerType
                .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
                .Cast<RouteAttribute>()
                .Select(attribute => attribute.Template)
                .ToHashSet(StringComparer.Ordinal);

            Assert.Equal(officialRoute, Assert.Single(routes));
        }

        public static IEnumerable<object[]> ControllersAndRoutes()
        {
            yield return new object[]
            {
                typeof(PedidoController), "api/v1/pedidos"
            };
            yield return new object[]
            {
                typeof(PecaController), "api/v1/pecas"
            };
            yield return new object[]
            {
                typeof(ServicoController), "api/v1/servicos"
            };
        }
    }
}
