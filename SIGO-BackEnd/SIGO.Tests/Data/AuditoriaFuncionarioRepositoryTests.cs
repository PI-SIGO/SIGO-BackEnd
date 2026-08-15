using Microsoft.EntityFrameworkCore;
using SIGO.Data;
using SIGO.Data.Repositories;
using SIGO.Objects.Enums;
using SIGO.Objects.Models;
using Xunit;

namespace SIGO.Tests.Data;

public sealed class AuditoriaFuncionarioRepositoryTests
{
    [Fact]
    public async Task Get_OficinaNaoDeveAcessarLogsDeFuncionarioDeOutraOficina()
    {
        await using var context = CreateContext();
        context.Funcionarios.AddRange(
            CreateEmployee(id: 10, oficinaId: 1, email: "funcionario.a@sigo.test", cpf: "11111111111"),
            CreateEmployee(id: 20, oficinaId: 2, email: "funcionario.b@sigo.test", cpf: "22222222222"));
        context.AuditoriasFuncionarios.AddRange(
            CreateAudit(id: 1, funcionarioId: 10, funcionarioNome: "Funcionário A"),
            CreateAudit(id: 2, funcionarioId: 20, funcionarioNome: "Funcionário B"));
        await context.SaveChangesAsync();
        var repository = new AuditoriaFuncionarioRepository(context);

        var logsVisiveis = await repository.Get(oficinaId: 1);
        var tentativaOutroFuncionario = await repository.Get(funcionarioId: 20, oficinaId: 1);

        var log = Assert.Single(logsVisiveis);
        Assert.Equal(10, log.FuncionarioId);
        Assert.Empty(tentativaOutroFuncionario);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static Funcionario CreateEmployee(int id, int oficinaId, string email, string cpf) => new()
    {
        Id = id,
        Nome = $"Funcionário {id}",
        Cpf = cpf,
        Cargo = "Mecânico",
        Email = email,
        Senha = "hash-de-teste",
        Role = "Funcionario",
        Situacao = Situacao.ATIVO,
        IdOficina = oficinaId
    };

    private static AuditoriaFuncionario CreateAudit(int id, int funcionarioId, string funcionarioNome) => new()
    {
        Id = id,
        FuncionarioId = funcionarioId,
        FuncionarioNome = funcionarioNome,
        Acao = "ATUALIZAR",
        Entidade = "Pedido",
        EntidadeId = 1,
        DataHora = DateTime.UtcNow
    };
}
