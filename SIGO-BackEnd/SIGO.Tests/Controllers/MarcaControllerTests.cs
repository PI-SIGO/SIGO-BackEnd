using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SIGO.Controllers;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Security;
using SIGO.Services.Interfaces;
using Xunit;

namespace SIGO.Tests.Controllers;

public class MarcaControllerTests
{
    private readonly Mock<IMarcaService> _service = new();
    private readonly MarcaController _controller;

    public MarcaControllerTests()
    {
        _controller = new MarcaController(_service.Object, Mock.Of<IMapper>());
    }

    [Fact]
    public void Controller_DevePermitirLeituraParaTodosOsPerfisAutenticados()
    {
        var attribute = Assert.Single(typeof(MarcaController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>());

        Assert.Equal(AuthorizationPolicies.SelfServiceAccess, attribute.Policy);
    }

    [Theory]
    [InlineData(nameof(MarcaController.Add))]
    [InlineData(nameof(MarcaController.Update))]
    [InlineData(nameof(MarcaController.Remove))]
    public void Escritas_DevemContinuarRestritasAOficina(string methodName)
    {
        var attribute = Assert.Single(typeof(MarcaController)
            .GetMethod(methodName)!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>());

        Assert.Equal(AuthorizationPolicies.FullAccess, attribute.Policy);
    }

    [Fact]
    public async Task GetAll_DeveRetornarPaginaLimitada()
    {
        _service.Setup(s => s.GetAll()).ReturnsAsync(new[]
        {
            CriarMarca(1),
            CriarMarca(2),
            CriarMarca(3)
        });

        var result = await _controller.GetAll(new PaginationRequest { Page = 2, PageSize = 1 });

        var ok = Assert.IsType<OkObjectResult>(result);
        var page = Assert.IsType<PagedResponse<MarcaDTO>>(ok.Value);
        Assert.Equal(3, page.TotalItems);
        Assert.Equal(2, Assert.Single(page.Items).Id);
    }

    [Fact]
    public async Task Add_DeveRetornarCreatedComRecursoCriado()
    {
        var input = CriarMarca(0);
        var created = CriarMarca(8);
        _service.Setup(s => s.CreateMarca(input)).ReturnsAsync(created);

        var result = await _controller.Add(input);

        var response = Assert.IsType<CreatedResult>(result);
        Assert.Equal("/api/v1/marcas/8", response.Location);
        Assert.Same(created, response.Value);
    }

    [Fact]
    public async Task GetByName_DeveRetornarPaginaVazia_QuandoNaoHaResultado()
    {
        _service.Setup(s => s.GetByName("inexistente")).ReturnsAsync(Array.Empty<MarcaDTO>());

        var result = await _controller.GetByName("inexistente");

        var ok = Assert.IsType<OkObjectResult>(result);
        var page = Assert.IsType<PagedResponse<MarcaDTO>>(ok.Value);
        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalPages);
    }

    [Fact]
    public async Task Update_DeveRetornarRecursoAtualizado()
    {
        var input = CriarMarca(0);
        var updated = CriarMarca(4);
        _service.Setup(s => s.UpdateMarca(input, 4)).ReturnsAsync(updated);

        var result = await _controller.Update(4, input);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(updated, ok.Value);
    }

    [Fact]
    public async Task Remove_DeveRetornarNoContent()
    {
        var result = await _controller.Remove(4);

        Assert.IsType<NoContentResult>(result);
        _service.Verify(s => s.Remove(4), Times.Once);
    }

    private static MarcaDTO CriarMarca(int id) => new()
    {
        Id = id,
        Nome = $"Marca {id}",
        Desc = "Descrição",
        TipoMarca = "Automóvel"
    };
}
