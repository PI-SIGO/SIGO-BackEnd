using SIGO.Data.Interfaces;
using SIGO.Objects.Models;

using Microsoft.EntityFrameworkCore;

namespace SIGO.Data.Repositories
{
    public class PecaRepository : GenericRepository<Peca>, IPecaRepository
    {
        private readonly AppDbContext _context;

        public PecaRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Peca>> GetByOficina(int oficinaId)
        {
            return await _context.Pecas
                .Where(p => p.IdOficina == oficinaId)
                .ToListAsync();
        }

        public async Task<Peca?> GetByIdForOficina(int id, int oficinaId)
        {
            return await _context.Pecas
                .FirstOrDefaultAsync(p => p.Id == id && p.IdOficina == oficinaId);
        }

        public async Task<IReadOnlyList<Peca>> GetByIdsAsync(
            IReadOnlyCollection<int> ids,
            CancellationToken cancellationToken = default)
        {
            if (ids.Count == 0)
                return Array.Empty<Peca>();

            return await _context.Pecas
                .AsNoTracking()
                .Where(piece => ids.Contains(piece.Id))
                .ToListAsync(cancellationToken);
        }
    }
}
