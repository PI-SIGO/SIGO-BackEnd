using Microsoft.EntityFrameworkCore;
using SIGO.Data.Interfaces;
using SIGO.Objects.Models;

namespace SIGO.Data.Repositories
{
    public class RegistroServicoRepository : IRegistroServicoRepository
    {
        private readonly AppDbContext _context;

        public RegistroServicoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<RegistroServico>> GetByVeiculoAsync(
            int veiculoId,
            DateTime? from = null,
            DateTime? to = null,
            string? tipoServico = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.RegistroServicos
                .Include(r => r.Veiculo)
                .Include(r => r.Oficina)
                .Include(r => r.Servico)
                .Include(r => r.PecasSubstituidas)
                .Where(r => r.VeiculoId == veiculoId)
                .AsQueryable();

            if (from.HasValue)
                query = query.Where(r => r.DataServico >= from.Value);
            if (to.HasValue)
                query = query.Where(r => r.DataServico <= to.Value);
            if (!string.IsNullOrEmpty(tipoServico))
                query = query.Where(r => r.Servico != null && r.Servico.Nome.Contains(tipoServico));

            return await query.OrderByDescending(r => r.DataServico).ToListAsync(cancellationToken);
        }

    }
}
