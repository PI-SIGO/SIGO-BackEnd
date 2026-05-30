using Microsoft.EntityFrameworkCore;
using SIGO.Data.Interfaces;
using SIGO.Objects.Models;

namespace SIGO.Data.Repositories
{
    public class ClienteOficinaRepository : IClienteOficinaRepository
    {
        private readonly AppDbContext _context;

        public ClienteOficinaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ExistsAsync(int oficinaId, int clienteId)
        {
            return await _context.ClienteOficinas
                .AnyAsync(co => co.OficinaId == oficinaId && co.ClienteId == clienteId && co.Ativo);
        }

        public async Task AddIfNotExistsAsync(int oficinaId, int clienteId)
        {
            var exists = await ExistsAsync(oficinaId, clienteId);
            if (exists)
                return;

            await _context.ClienteOficinas.AddAsync(new ClienteOficina
            {
                OficinaId = oficinaId,
                ClienteId = clienteId,
                Ativo = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        public async Task AddOrUpdateVinculoAsync(int oficinaId, int clienteId)
        {
            var agora = DateTime.UtcNow;
            var relacionamento = await _context.ClienteOficinas
                .FirstOrDefaultAsync(co => co.OficinaId == oficinaId && co.ClienteId == clienteId);

            if (relacionamento is null)
            {
                await _context.ClienteOficinas.AddAsync(new ClienteOficina
                {
                    OficinaId = oficinaId,
                    ClienteId = clienteId,
                    Ativo = true,
                    CreatedAt = agora,
                    UpdatedAt = agora
                });
            }
            else
            {
                relacionamento.Ativo = true;
                relacionamento.UpdatedAt = agora;
            }

            await _context.SaveChangesAsync();
        }
    }
}
