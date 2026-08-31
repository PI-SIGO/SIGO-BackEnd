using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SIGO.Data;
using SIGO.Data.Seed;
using SIGO.Objects.Models;
using SIGO.Security;
using Xunit;

namespace SIGO.Tests.Data;

public sealed class DevelopmentDatabaseSeederTests
{
    [Fact]
    public async Task SeedAsync_CreatesCompleteDataset_WithoutDuplicatingData()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"sigo-seed-{Guid.NewGuid()}")
            .Options;

        await using var context = new AppDbContext(options);
        var passwordHasher = new TestPasswordHasher();
        var seeder = new DevelopmentDatabaseSeeder(
            context,
            passwordHasher,
            NullLogger<DevelopmentDatabaseSeeder>.Instance);

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        Assert.Equal(2, await context.Oficinas.CountAsync());
        Assert.Equal(2, await context.Clientes.CountAsync());
        Assert.Equal(2, await context.ClienteContas.CountAsync());
        Assert.Equal(2, await context.Funcionarios.CountAsync());
        Assert.Equal(2, await context.Veiculos.CountAsync());
        Assert.Equal(2, await context.Servicos.CountAsync());
        Assert.Single(await context.Marcas.ToListAsync());
        Assert.Single(await context.Pecas.ToListAsync());
        Assert.Single(await context.Pedidos.ToListAsync());
        Assert.Equal(2, await context.ClienteOficinas.CountAsync());
        Assert.Equal(2, await context.Set<Funcionario_Servico>().CountAsync());

        var cliente = await context.Clientes.SingleAsync(
            item => item.Email == DevelopmentDatabaseSeeder.ClienteEmail);
        var conta = await context.ClienteContas.SingleAsync(
            item => item.ClienteId == cliente.Id);

        Assert.True(passwordHasher.Verify(
            DevelopmentDatabaseSeeder.ClientePassword,
            conta.PasswordHash));
        Assert.Contains(
            await context.Veiculos.ToListAsync(),
            veiculo => veiculo.ClienteId == cliente.Id && veiculo.PlacaVeiculo == "SED1A23");
    }

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        public string Hash(string input) => $"hashed:{input}";

        public bool Verify(string input, string hashedValue) =>
            hashedValue == Hash(input);

        public bool NeedsRehash(string hashedValue) => false;
    }
}
