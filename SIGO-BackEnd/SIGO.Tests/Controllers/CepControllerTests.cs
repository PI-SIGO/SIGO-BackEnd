using Microsoft.AspNetCore.Authorization;
using Moq;
using SIGO.Controllers;
using SIGO.Integracao.Interfaces;
using Xunit;

namespace SIGO.Tests.Controllers;

public sealed class CepControllerTests
{
    [Fact]
    public void Controller_DevePermitirConsultaAnonima()
    {
        var attribute = Assert.Single(typeof(CepController)
            .GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: false)
            .Cast<AllowAnonymousAttribute>());

        Assert.NotNull(attribute);
    }

    [Fact]
    public async Task ListarDadosEndereco_DeveRetornarNotFound_QuandoCepNaoExiste()
    {
        var integration = new Mock<IViaCepIntegracao>();
        integration
            .Setup(service => service.ObterDadosViaCep("00000000"))
            .ReturnsAsync((SIGO.Integracao.Response.ViaCepResponse?)null);
        var controller = new CepController(integration.Object);

        var result = await controller.ListarDadosEndereco("00000000");

        Assert.IsType<Microsoft.AspNetCore.Mvc.ObjectResult>(result);
        var response = (Microsoft.AspNetCore.Mvc.ObjectResult)result;
        Assert.Equal(404, response.StatusCode);
    }
}
