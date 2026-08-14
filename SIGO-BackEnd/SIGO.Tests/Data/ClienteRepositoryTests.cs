using Microsoft.EntityFrameworkCore;
using SIGO.Data;
using SIGO.Data.Repositories;
using SIGO.Objects.Enums;
using SIGO.Objects.Models;
using Xunit;

namespace SIGO.Tests.Data
{
    public class ClienteRepositoryTests
    {
        [Fact]
        public async Task GetByOficina_DeveRetornarVinculosAtivosEInativosDaMesmaOficina()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            await using var context = new AppDbContext(options);
            context.Clientes.AddRange(
                CreateClient(1, Situacao.ATIVO, oficinaId: 2, vinculoAtivo: true),
                CreateClient(2, Situacao.INATIVO, oficinaId: 2, vinculoAtivo: false),
                CreateClient(3, Situacao.ATIVO, oficinaId: 2, vinculoAtivo: false),
                CreateClient(4, Situacao.INATIVO, oficinaId: 3, vinculoAtivo: false));
            await context.SaveChangesAsync();
            var repository = new ClienteRepository(context);

            var result = (await repository.GetByOficina(2)).ToArray();

            Assert.Equal(new[] { 1, 2, 3 }, result.Select(cliente => cliente.Id).OrderBy(id => id));
        }

        private static Cliente CreateClient(
            int id,
            Situacao situacao,
            int oficinaId,
            bool vinculoAtivo)
        {
            var cliente = new Cliente
            {
                Id = id,
                Nome = $"Cliente {id}",
                Cpf_Cnpj = $"0000000000{id}",
                Situacao = situacao,
                TipoCliente = TipoCliente.FISICO,
                ClienteOficinas = new List<ClienteOficina>()
            };
            cliente.ClienteOficinas.Add(new ClienteOficina
            {
                ClienteId = id,
                Cliente = cliente,
                OficinaId = oficinaId,
                Ativo = vinculoAtivo,
                RevogadoEm = vinculoAtivo ? null : DateTime.UtcNow
            });

            return cliente;
        }
    }
}
