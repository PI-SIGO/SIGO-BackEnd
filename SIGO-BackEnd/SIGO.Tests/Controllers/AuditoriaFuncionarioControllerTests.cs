using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGO.Controllers;
using SIGO.Security;
using Xunit;

namespace SIGO.Tests.Controllers;

public sealed class AuditoriaFuncionarioControllerTests
{
    [Fact]
    public void Controller_DeveExporRotaOficialV1()
    {
        var route = typeof(AuditoriaFuncionarioController)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Single();

        Assert.Equal("api/v1/auditoria-funcionarios", route.Template);
    }

    [Fact]
    public void Oficina_DeveTerAcoesGetAutorizadasParaConsultarLogsDosFuncionarios()
    {
        var getActions = typeof(AuditoriaFuncionarioController)
            .GetMethods()
            .Where(method => method.DeclaringType == typeof(AuditoriaFuncionarioController))
            .Where(method => method.GetCustomAttributes(typeof(HttpGetAttribute), inherit: false).Any())
            .ToArray();

        Assert.Equal(2, getActions.Length);
        Assert.Contains(getActions, action => action.Name == nameof(AuditoriaFuncionarioController.Get));
        Assert.Contains(getActions, action => action.Name == nameof(AuditoriaFuncionarioController.GetByFuncionario));

        var authorize = typeof(AuditoriaFuncionarioController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal(AuthorizationPolicies.FullAccess, authorize.Policy);
    }
}
