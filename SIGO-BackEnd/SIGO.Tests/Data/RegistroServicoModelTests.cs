using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SIGO.Data;
using SIGO.Objects.Models;
using Xunit;

namespace SIGO.Tests.Data;

public sealed class RegistroServicoModelTests
{
    [Fact]
    public void Model_DevePersistirOficinaObrigatoriaEImutavel()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=model-only;Username=model;Password=model")
            .Options;
        using var context = new AppDbContext(options);

        var entity = context.Model.FindEntityType(typeof(RegistroServico));
        var officeProperty = entity?.FindProperty(nameof(RegistroServico.OficinaId));
        var officeForeignKey = entity?.GetForeignKeys().SingleOrDefault(foreignKey =>
            foreignKey.Properties.SingleOrDefault()?.Name == nameof(RegistroServico.OficinaId));

        Assert.NotNull(officeProperty);
        Assert.False(officeProperty!.IsNullable);
        Assert.Equal(PropertySaveBehavior.Throw, officeProperty.GetAfterSaveBehavior());
        Assert.NotNull(officeForeignKey);
        Assert.Equal(typeof(Oficina), officeForeignKey!.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Restrict, officeForeignKey.DeleteBehavior);
    }
}
