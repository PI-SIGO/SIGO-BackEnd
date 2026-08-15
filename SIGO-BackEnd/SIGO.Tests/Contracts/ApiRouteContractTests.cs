using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using SIGO.Controllers;
using SIGO.Objects.Dtos.Entities;
using Xunit;

namespace SIGO.Tests.Contracts;

public sealed class ApiRouteContractTests
{
    [Fact]
    public void Controllers_DevemExporSomenteUmaRotaBaseOficialV1()
    {
        var controllerTypes = typeof(ClienteController).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type))
            .ToArray();

        var invalidRoutes = controllerTypes
            .Select(type => new
            {
                Controller = type.Name,
                Routes = type
                .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
                .Cast<RouteAttribute>()
                .Select(route => route.Template)
                .ToArray()
            })
            .Where(item =>
                item.Routes.Length != 1 ||
                item.Routes[0]?.StartsWith("api/v1/", StringComparison.OrdinalIgnoreCase) != true)
            .Select(item => $"{item.Controller}: {string.Join(", ", item.Routes)}")
            .ToArray();

        Assert.Empty(invalidRoutes);
    }

    [Fact]
    public void Actions_NaoDevemExporAliasesAbsolutosSemVersao()
    {
        var invalidRoutes = typeof(ClienteController).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type))
            .SelectMany(type => type
                .GetMethods()
                .SelectMany(method => method
                    .GetCustomAttributes(typeof(HttpMethodAttribute), inherit: false)
                    .Cast<HttpMethodAttribute>()
                    .Select(attribute => new
                    {
                        Controller = type.Name,
                        Action = method.Name,
                        Route = attribute.Template
                    })))
            .Where(item =>
            {
                var normalizedRoute = item.Route?.TrimStart('~', '/');
                return normalizedRoute?.StartsWith("api/", StringComparison.OrdinalIgnoreCase) == true &&
                    normalizedRoute.StartsWith("api/v1/", StringComparison.OrdinalIgnoreCase) == false;
            })
            .Select(item => $"{item.Controller}.{item.Action}: {item.Route}")
            .ToArray();

        Assert.Empty(invalidRoutes);
    }

    [Fact]
    public void Endpoints_NaoDevemDuplicarMetodoERotaNoSwagger()
    {
        var endpoints = typeof(ClienteController).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type))
            .SelectMany(controllerType =>
            {
                var controllerRoute = controllerType
                    .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
                    .Cast<RouteAttribute>()
                    .Single()
                    .Template!;

                return controllerType
                    .GetMethods()
                    .SelectMany(method => method
                        .GetCustomAttributes(typeof(HttpMethodAttribute), inherit: false)
                        .Cast<HttpMethodAttribute>()
                        .SelectMany(attribute => attribute.HttpMethods.Select(httpMethod =>
                        {
                            var actionRoute = attribute.Template;
                            var fullRoute = actionRoute?.StartsWith("~/", StringComparison.Ordinal) == true ||
                                            actionRoute?.StartsWith("/", StringComparison.Ordinal) == true
                                ? actionRoute.TrimStart('~', '/')
                                : string.IsNullOrWhiteSpace(actionRoute)
                                    ? controllerRoute
                                    : $"{controllerRoute.TrimEnd('/')}/{actionRoute.TrimStart('/')}";

                            return $"{httpMethod.ToUpperInvariant()} {fullRoute.TrimEnd('/').ToLowerInvariant()}";
                        })));
            })
            .ToArray();

        var duplicates = endpoints
            .GroupBy(endpoint => endpoint, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void ClienteVinculoController_NaoDeveExporAprovacaoConsentimentoOuPendencia()
    {
        var routeTemplates = typeof(ClienteVinculoController)
            .GetMethods()
            .SelectMany(method => method
                .GetCustomAttributes(typeof(HttpMethodAttribute), inherit: false)
                .Cast<HttpMethodAttribute>())
            .Select(attribute => attribute.Template ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain(routeTemplates, route => route.Contains("aprov", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(routeTemplates, route => route.Contains("consent", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(routeTemplates, route => route.Contains("pendente", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CadastroCompletoDaOficina_DeveUsarPostRaizDeClientes()
    {
        var controllerRoute = Assert.Single(typeof(ClienteVinculoController)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>());
        var actionRoute = Assert.Single(typeof(ClienteVinculoController)
            .GetMethod(nameof(ClienteVinculoController.RegisterFull))!
            .GetCustomAttributes(typeof(HttpPostAttribute), inherit: false)
            .Cast<HttpPostAttribute>());

        Assert.Equal("api/v1/clientes", controllerRoute.Template);
        Assert.Null(actionRoute.Template);
    }

    [Fact]
    public void CadastroCompletoDaOficina_NaoDevePermitirDefinirCredenciaisOuEscopo()
    {
        var propertyNames = typeof(PreCadastrarClienteDTO)
            .GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.DoesNotContain("Senha", propertyNames);
        Assert.DoesNotContain("Password", propertyNames);
        Assert.DoesNotContain("Situacao", propertyNames);
        Assert.DoesNotContain("ClienteId", propertyNames);
        Assert.DoesNotContain("OficinaId", propertyNames);
        Assert.DoesNotContain("Conta", propertyNames);
    }

    [Fact]
    public void LoginDoCliente_DeveAceitarDocumentoESenha()
    {
        var propertyNames = typeof(LoginClienteDTO)
            .GetProperties()
            .Where(property => !Attribute.IsDefined(property, typeof(JsonIgnoreAttribute)))
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.True(propertyNames.SetEquals(new[]
        {
            nameof(LoginClienteDTO.Cpf),
            nameof(LoginClienteDTO.Cpf_Cnpj),
            nameof(LoginClienteDTO.Senha)
        }));
    }
}
