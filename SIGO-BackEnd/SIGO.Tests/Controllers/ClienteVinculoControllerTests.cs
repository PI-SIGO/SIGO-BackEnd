using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SIGO.Controllers;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Enums;
using SIGO.Security;
using SIGO.Services.Interfaces;
using Xunit;

namespace SIGO.Tests.Controllers;

public sealed class ClienteVinculoControllerTests
{
    private readonly Mock<IClienteVinculoService> _vinculoServiceMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();

    [Fact]
    public async Task RegisterFull_DeveRetornarForbid_QuandoJwtNaoTemOficinaId()
    {
        var controller = CreateController(9, null, new[] { SystemRoles.Oficina });

        var result = await controller.RegisterFull(CreatePreRegistration(), CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
        _vinculoServiceMock.Verify(service => service.PreRegisterAsync(
            It.IsAny<PreCadastrarClienteDTO>(),
            It.IsAny<int>(),
            It.IsAny<SecurityAuditContext>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterFull_DeveCriarClienteCompletoEVinculoAtivoComOficinaDoJwt()
    {
        var request = CreatePreRegistration();
        var response = new PreCadastroClienteResultadoDTO(42, request.Nome, request.Cpf, true);
        _vinculoServiceMock
            .Setup(service => service.PreRegisterAsync(
                request,
                7,
                It.IsAny<SecurityAuditContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        var controller = CreateController(9, 7, new[] { SystemRoles.Oficina });

        var result = await controller.RegisterFull(request, CancellationToken.None);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.Same(response, created.Value);
        Assert.True(response.VinculoAtivo);
        _vinculoServiceMock.Verify(service => service.PreRegisterAsync(
            request,
            7,
            It.Is<SecurityAuditContext>(context =>
                context.TipoAtor == TipoAtorAuditoria.Oficina &&
                context.AtorId == 9 &&
                context.CorrelationId == "vinculo-request"),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task RevokeLink_DeveUsarClienteDoJwt()
    {
        var controller = CreateController(42, null, new[] { SystemRoles.Cliente });

        var result = await controller.RevokeLink(7, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        _vinculoServiceMock.Verify(service => service.RevokeAsync(
            42,
            7,
            It.IsAny<SecurityAuditContext>(),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task DeactivateLinkForOficina_DeveDesativarSomenteVinculoDaOficinaDoJwt()
    {
        var controller = CreateController(7, 7, new[] { SystemRoles.Oficina });

        var result = await controller.DeactivateLinkForOficina(42, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        _vinculoServiceMock.Verify(service => service.DeactivateForOficinaAsync(
            42,
            7,
            It.Is<SecurityAuditContext>(context =>
                context.TipoAtor == TipoAtorAuditoria.Oficina &&
                context.AtorId == 7),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task DeactivateLinkForOficina_DeveRetornarForbid_QuandoJwtNaoTemOficinaId()
    {
        var controller = CreateController(7, null, new[] { SystemRoles.Oficina });

        var result = await controller.DeactivateLinkForOficina(42, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
        _vinculoServiceMock.Verify(service => service.DeactivateForOficinaAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<SecurityAuditContext>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateLinkStatusForOficina_DeveUsarOficinaDoJwt()
    {
        var request = new AtualizarStatusVinculoClienteRequestDTO { Ativo = true };
        _vinculoServiceMock
            .Setup(service => service.UpdateStatusForOficinaAsync(
                42,
                7,
                true,
                It.IsAny<SecurityAuditContext>(),
                CancellationToken.None))
            .ReturnsAsync(true);
        var controller = CreateController(9, 7, new[] { SystemRoles.Oficina });

        var result = await controller.UpdateLinkStatusForOficina(
            42,
            request,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<StatusVinculoClienteOficinaDTO>(ok.Value);
        Assert.Equal(42, response.ClienteId);
        Assert.True(response.Ativo);
        _vinculoServiceMock.Verify(service => service.UpdateStatusForOficinaAsync(
            42,
            7,
            true,
            It.Is<SecurityAuditContext>(context =>
                context.TipoAtor == TipoAtorAuditoria.Oficina &&
                context.AtorId == 9),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task UpdateLinkStatusForOficina_DeveRejeitarStatusAusente()
    {
        var controller = CreateController(9, 7, new[] { SystemRoles.Oficina });

        var result = await controller.UpdateLinkStatusForOficina(
            42,
            new AtualizarStatusVinculoClienteRequestDTO(),
            CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, problem.StatusCode);
        _vinculoServiceMock.Verify(service => service.UpdateStatusForOficinaAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<bool>(),
            It.IsAny<SecurityAuditContext>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private ClienteVinculoController CreateController(int? userId, int? oficinaId, string[] roles)
    {
        _currentUserServiceMock.Setup(service => service.UserId).Returns(userId);
        _currentUserServiceMock.Setup(service => service.OficinaId).Returns(oficinaId);
        _currentUserServiceMock
            .Setup(service => service.IsInRole(It.IsAny<string>()))
            .Returns<string>(roles.Contains);

        var httpContext = new DefaultHttpContext { TraceIdentifier = "vinculo-request" };
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;

        return new ClienteVinculoController(
            _vinculoServiceMock.Object,
            _currentUserServiceMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private static PreCadastrarClienteDTO CreatePreRegistration() => new()
    {
        Cpf = "52998224725",
        Nome = "Cliente Completo",
        Email = "cliente@example.com",
        Obs = "Prefere contato pela manhã",
        Razao = "",
        DataNasc = new DateOnly(1950, 5, 10),
        Sexo = Sexo.Masculino,
        Numero = 120,
        Rua = "Rua das Flores",
        Cidade = "Blumenau",
        Cep = "89010-000",
        Bairro = "Centro",
        Estado = "SC",
        Pais = "Brasil",
        Complemento = "Casa",
        Telefones = new[]
        {
            new PreCadastrarTelefoneClienteDTO { DDD = 47, Numero = "99999-9999" }
        }
    };
}
