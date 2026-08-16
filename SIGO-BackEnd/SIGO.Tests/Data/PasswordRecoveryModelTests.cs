using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using SIGO.Data;
using SIGO.Objects.Models;
using Xunit;

namespace SIGO.Tests.Data;

public sealed class PasswordRecoveryModelTests
{
    [Fact]
    public void Model_DevePersistirSomenteHashComExpiracaoEUsoUnico()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=model-only;Username=model;Password=model")
            .Options;
        using var context = new AppDbContext(options);

        var entity = context.GetService<IDesignTimeModel>()
            .Model
            .FindEntityType(typeof(TokenRedefinicaoSenha));

        Assert.NotNull(entity);
        Assert.NotNull(entity!.FindProperty(nameof(TokenRedefinicaoSenha.TokenHash)));
        Assert.Equal(
            64,
            entity.FindProperty(nameof(TokenRedefinicaoSenha.TokenHash))!.GetMaxLength());
        Assert.NotNull(entity.FindProperty(nameof(TokenRedefinicaoSenha.ExpiraEm)));
        Assert.NotNull(entity.FindProperty(nameof(TokenRedefinicaoSenha.UsadoEm)));
        Assert.DoesNotContain(
            entity.GetProperties(),
            property => property.Name.Contains("Raw", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            entity.GetIndexes(),
            index =>
                index.IsUnique &&
                index.Properties.Single().Name == nameof(TokenRedefinicaoSenha.TokenHash));
    }
}
