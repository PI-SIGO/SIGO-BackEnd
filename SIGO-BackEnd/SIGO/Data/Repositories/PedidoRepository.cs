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

        public override async Task<IEnumerable<Pedido>> Get()
        {
            return await PedidosComDetalhes().ToListAsync();
        }

        public async Task<IEnumerable<Pedido>> GetByOficina(int oficinaId)
        {
            return await PedidosComDetalhes()
                .Where(p => p.idOficina == oficinaId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Pedido>> GetByCliente(int clienteId)
        {
            return await PedidosComDetalhes()
                .Where(p => p.idCliente == clienteId)
                .ToListAsync();
        }

        public async Task<Pedido?> GetByIdForOficina(int id, int oficinaId)
        {
            return await PedidosComDetalhes()
                .FirstOrDefaultAsync(p => p.Id == id && p.idOficina == oficinaId);
        }

        public async Task<IEnumerable<Pedido>> GetByVeiculoWithDetailsAsync(int veiculoId)
        {
            return await PedidosComDetalhes()
                .Where(p => p.idVeiculo == veiculoId)
                .OrderByDescending(p => p.DataInicio)
                .ToListAsync();
        }

        private IQueryable<Pedido> PedidosComDetalhes()
        {
            return _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Veiculo)
                .Include(p => p.Oficina)
                .Include(p => p.Funcionario)
                .Include(p => p.Pedido_Servicos).ThenInclude(ps => ps.Servico)
                .Include(p => p.Pedido_Pecas).ThenInclude(pp => pp.Peca);
        }
    }
}
