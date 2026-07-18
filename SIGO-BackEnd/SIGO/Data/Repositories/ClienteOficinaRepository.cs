using Microsoft.EntityFrameworkCore;
using SIGO.Data.Interfaces;
using SIGO.Exceptions;
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
                .AnyAsync(co =>
                    co.OficinaId == oficinaId &&
                    co.ClienteId == clienteId &&
                    co.Ativo &&
                    co.Cliente.Situacao == SIGO.Objects.Enums.Situacao.ATIVO);
        }

        public Task<ClienteOficina?> GetAsync(
            int oficinaId,
            int clienteId,
            CancellationToken cancellationToken = default)
        {
            return _context.ClienteOficinas.FirstOrDefaultAsync(
                relacionamento => relacionamento.OficinaId == oficinaId &&
                                  relacionamento.ClienteId == clienteId,
                cancellationToken);
        }

        public async Task<IReadOnlyList<ClienteOficina>> GetByClienteAsync(
            int clienteId,
            CancellationToken cancellationToken = default)
        {
            return await _context.ClienteOficinas
                .AsNoTracking()
                .Include(relacionamento => relacionamento.Oficina)
                .Where(relacionamento => relacionamento.ClienteId == clienteId)
                .OrderByDescending(relacionamento => relacionamento.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<ClienteOficina> AddOrActivateAsync(
            int oficinaId,
            int clienteId,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var relacionamento = await GetAsync(oficinaId, clienteId, cancellationToken);

            if (relacionamento is null)
            {
                relacionamento = new ClienteOficina
                {
                    OficinaId = oficinaId,
                    ClienteId = clienteId,
                    Ativo = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                await _context.ClienteOficinas.AddAsync(relacionamento, cancellationToken);
            }
            else
            {
                if (relacionamento.RevogadoEm.HasValue)
                {
                    throw new ConflictException(
                        "O cliente revogou este vinculo. A oficina nao pode reativa-lo automaticamente.");
                }

                relacionamento.Ativo = true;
                relacionamento.UpdatedAt = now;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return relacionamento;
        }

        public async Task<bool> DeactivateAsync(
            int oficinaId,
            int clienteId,
            DateTime updatedAt,
            CancellationToken cancellationToken = default)
        {
            var relacionamento = await GetAsync(oficinaId, clienteId, cancellationToken);
            if (relacionamento is null)
                return false;

            relacionamento.Ativo = false;
            relacionamento.UpdatedAt = updatedAt;
            relacionamento.RevogadoEm = updatedAt;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
