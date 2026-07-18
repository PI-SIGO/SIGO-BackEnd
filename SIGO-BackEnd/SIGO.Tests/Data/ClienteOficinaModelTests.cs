using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using SIGO.Data;
using SIGO.Objects.Models;
using Xunit;

namespace SIGO.Tests.Data;

public sealed class ClienteOficinaModelTests
{
    [Fact]
    public void Model_DeveRepresentarVinculoDiretoSemAprovacaoOuConsentimento()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=model-only;Username=model;Password=model")
            .Options;
        using var context = new AppDbContext(options);

        var entity = context.GetService<IDesignTimeModel>()
            .Model
            .FindEntityType(typeof(ClienteOficina));

        Assert.NotNull(entity);
        Assert.NotNull(entity!.FindProperty(nameof(ClienteOficina.Ativo)));
        Assert.NotNull(entity.FindProperty(nameof(ClienteOficina.RevogadoEm)));
        Assert.Contains(
            entity.GetCheckConstraints(),
            constraint => constraint.Name == "CK_cliente_oficina_ativo_revogado");
        Assert.Null(entity.FindProperty("Estado"));
        Assert.Null(entity.FindProperty("ConsentimentoMotivo"));
        Assert.Null(entity.FindProperty("ConsentimentoForma"));
        Assert.Null(entity.FindProperty("ConsentimentoEvidencia"));
        Assert.Equal(
            new[] { nameof(ClienteOficina.OficinaId), nameof(ClienteOficina.ClienteId) },
            entity.FindPrimaryKey()!.Properties.Select(property => property.Name));
    }
}
