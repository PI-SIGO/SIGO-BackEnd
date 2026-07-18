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

        public async Task<Pedido?> GetByIdWithDetails(int id)
        {
            return await PedidosComDetalhes()
                .FirstOrDefaultAsync(p => p.Id == id);
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

        public async Task SaveWithDetailsAsync(
            Pedido pedido,
            IReadOnlyCollection<Pedido_Peca> pecas,
            IReadOnlyCollection<Pedido_Servico> servicos,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            SynchronizePieces(pedido, pecas);
            SynchronizeServices(pedido, servicos);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        private void SynchronizePieces(
            Pedido pedido,
            IReadOnlyCollection<Pedido_Peca> requestedPieces)
        {
            var requestedByPieceId = requestedPieces.ToDictionary(piece => piece.IdPeca);

            foreach (var existing in pedido.Pedido_Pecas.ToArray())
            {
                if (!requestedByPieceId.TryGetValue(existing.IdPeca, out var requested))
                {
                    _context.Set<Pedido_Peca>().Remove(existing);
                    continue;
                }

                existing.Quantidade = requested.Quantidade;
                existing.DataInstalacao = requested.DataInstalacao;
                existing.Estado = requested.Estado;
                existing.Observacao = requested.Observacao;
            }

            var existingIds = pedido.Pedido_Pecas.Select(piece => piece.IdPeca).ToHashSet();
            foreach (var requested in requestedPieces.Where(piece => !existingIds.Contains(piece.IdPeca)))
            {
                pedido.Pedido_Pecas.Add(new Pedido_Peca
                {
                    IdPedido = pedido.Id,
                    IdPeca = requested.IdPeca,
                    Quantidade = requested.Quantidade,
                    DataInstalacao = requested.DataInstalacao,
                    Estado = requested.Estado,
                    Observacao = requested.Observacao
                });
            }
        }

        private void SynchronizeServices(
            Pedido pedido,
            IReadOnlyCollection<Pedido_Servico> requestedServices)
        {
            var requestedByServiceId = requestedServices.ToDictionary(service => service.IdServico);

            foreach (var existing in pedido.Pedido_Servicos.ToArray())
            {
                if (!requestedByServiceId.TryGetValue(existing.IdServico, out var requested))
                {
                    _context.Set<Pedido_Servico>().Remove(existing);
                    continue;
                }

                existing.QuantVezes = requested.QuantVezes;
            }

            var existingIds = pedido.Pedido_Servicos.Select(service => service.IdServico).ToHashSet();
            foreach (var requested in requestedServices.Where(service => !existingIds.Contains(service.IdServico)))
            {
                pedido.Pedido_Servicos.Add(new Pedido_Servico
                {
                    IdPedido = pedido.Id,
                    IdServico = requested.IdServico,
                    QuantVezes = requested.QuantVezes
                });
            }
        }

        private IQueryable<Pedido> PedidosComDetalhes()
        {
            return _context.Pedidos
                .AsSplitQuery()
                .Include(p => p.Cliente)
                .Include(p => p.Veiculo)
                .Include(p => p.Oficina)
                .Include(p => p.Funcionario)
                .Include(p => p.Pedido_Servicos).ThenInclude(ps => ps.Servico)
                .Include(p => p.Pedido_Pecas).ThenInclude(pp => pp.Peca);
        }
    }
}
