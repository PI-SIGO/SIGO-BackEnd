using Microsoft.EntityFrameworkCore;
using SIGO.Data;
using SIGO.Data.Repositories;
using SIGO.Exceptions;
using SIGO.Objects.Models;
using Xunit;

namespace SIGO.Tests.Data;

public sealed class ClienteOficinaRepositoryTests
{
    [Fact]
    public async Task DeactivateByOficinaAsync_DevePermitirReativacaoPosterior()
    {
        await using var context = CreateContext();
        context.ClienteOficinas.Add(new ClienteOficina
        {
            OficinaId = 3,
            ClienteId = 6,
            Ativo = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var repository = new ClienteOficinaRepository(context);

        var desativado = await repository.DeactivateByOficinaAsync(
            oficinaId: 3,
            clienteId: 6,
            updatedAt: DateTime.UtcNow);

        var vinculoDesativado = await context.ClienteOficinas.SingleAsync();
        Assert.True(desativado);
        Assert.False(vinculoDesativado.Ativo);
        Assert.Null(vinculoDesativado.RevogadoEm);

        var vinculoReativado = await repository.AddOrActivateAsync(oficinaId: 3, clienteId: 6);

        Assert.True(vinculoReativado.Ativo);
        Assert.Null(vinculoReativado.RevogadoEm);
    }

    [Fact]
    public async Task DeactivateAsync_QuandoRevogadoPeloCliente_DeveBloquearReativacaoAutomatica()
    {
        await using var context = CreateContext();
        context.ClienteOficinas.Add(new ClienteOficina
        {
            OficinaId = 3,
            ClienteId = 6,
            Ativo = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var repository = new ClienteOficinaRepository(context);
        await repository.DeactivateAsync(
            oficinaId: 3,
            clienteId: 6,
            updatedAt: DateTime.UtcNow);

        var vinculoRevogado = await context.ClienteOficinas.SingleAsync();
        Assert.False(vinculoRevogado.Ativo);
        Assert.NotNull(vinculoRevogado.RevogadoEm);

        await Assert.ThrowsAsync<ConflictException>(
            () => repository.AddOrActivateAsync(oficinaId: 3, clienteId: 6));
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
