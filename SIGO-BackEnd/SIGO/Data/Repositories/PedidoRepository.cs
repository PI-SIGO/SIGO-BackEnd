using Microsoft.EntityFrameworkCore;
using SIGO.Data.Interfaces;
using SIGO.Objects.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace SIGO.Data.Repositories
{
    public class PedidoRepository : GenericRepository<Pedido>, IPedidoRepository
    {
        private readonly AppDbContext _context;

        public PedidoRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Pedido>> GetByVeiculoWithDetailsAsync(int veiculoId)
        {
            return await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Veiculo)
                .Include(p => p.Oficina)
                .Include(p => p.Pedido_Servicos).ThenInclude(ps => ps.Servico)
                .Include(p => p.Pedido_Pecas).ThenInclude(pp => pp.Peca)
                .Where(p => p.idVeiculo == veiculoId)
                .OrderByDescending(p => p.DataInicio)
                .ToListAsync();
        }
    }
}
