using Microsoft.EntityFrameworkCore;
using SIGO.Data.Interfaces;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;

namespace SIGO.Data.Repositories
{
    public class TelefoneRepository : GenericRepository<Telefone>, ITelefoneRepository
    {
        private readonly AppDbContext _context;

        public TelefoneRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TelefoneDTO>> GetTelefoneByNome(string nome)
        {
            return await _context.Telefones
                .Where(t => t.Clientes.Nome.Contains(nome))
                .Select(t => new TelefoneDTO
                {
                    Id = t.Id,
                    Numero = t.Numero,
                    DDD = t.DDD,
                    ClienteId = t.ClienteId
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<TelefoneDTO>> GetTelefoneByNomeForOficina(string nome, int oficinaId)
        {
            return await _context.Telefones
                .Where(t =>
                    t.Clientes.Nome.Contains(nome) &&
                    t.Clientes.ClienteOficinas.Any(co =>
                        co.OficinaId == oficinaId &&
                        co.Ativo &&
                        co.DadosPermitidos.Contains("\"Telefones\"")))
                .Select(t => new TelefoneDTO
                {
                    Id = t.Id,
                    Numero = t.Numero,
                    DDD = t.DDD,
                    ClienteId = t.ClienteId
                })
                .ToListAsync();
        }

        public async Task<IReadOnlyCollection<int>> GetInvalidIdsForCliente(int clienteId, IEnumerable<int> telefoneIds)
        {
            var ids = telefoneIds
                .Where(id => id > 0)
                .Distinct()
                .ToArray();

            if (ids.Length == 0)
                return Array.Empty<int>();

            var ownedIds = await _context.Telefones
                .Where(t => ids.Contains(t.Id) && t.ClienteId == clienteId)
                .Select(t => t.Id)
                .ToListAsync();

            var ownedIdSet = ownedIds.ToHashSet();
            return ids
                .Where(id => !ownedIdSet.Contains(id))
                .ToArray();
        }

        public async Task<bool> UpdateForCliente(Telefone telefone, int clienteId)
        {
            var existingTelefone = await _context.Telefones
                .FirstOrDefaultAsync(t => t.Id == telefone.Id && t.ClienteId == clienteId);

            if (existingTelefone is null)
                return false;

            existingTelefone.DDD = telefone.DDD;
            existingTelefone.Numero = telefone.Numero;

            await SaveChanges();
            return true;
        }
    }
}
