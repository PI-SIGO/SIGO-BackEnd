using Microsoft.EntityFrameworkCore;
using SIGO.Data.Interfaces;
using SIGO.Objects.Models;

namespace SIGO.Data.Repositories
{
    public class OpcaoCadastroRepository : IOpcaoCadastroRepository
    {
        private readonly AppDbContext _context;

        public OpcaoCadastroRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<OpcaoCadastro>> GetByOficinaAsync(
            int oficinaId,
            CancellationToken cancellationToken = default)
        {
            return await _context.OpcoesCadastro
                .AsNoTracking()
                .Where(option => option.IdOficina == oficinaId)
                .OrderBy(option => option.Categoria)
                .ThenBy(option => option.Valor)
                .ToListAsync(cancellationToken);
        }
    }
}
