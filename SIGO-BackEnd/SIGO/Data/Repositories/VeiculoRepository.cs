using Microsoft.EntityFrameworkCore;
using SIGO.Data.Interfaces;
using SIGO.Objects.Models;

namespace SIGO.Data.Repositories
{
    public class VeiculoRepository : GenericRepository<Veiculo>, IVeiculoRepository
    {
        private readonly AppDbContext _context;

        public VeiculoRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public override async Task<IEnumerable<Veiculo>> Get()
        {
            return await VeiculosComDetalhes().ToListAsync();
        }

        public async Task<IEnumerable<Veiculo>> GetByPlaca(string placa)
        {
            return await VeiculosComDetalhes()
                .Where(v => v.PlacaVeiculo.Contains(placa))
                .ToListAsync();
        }

        public async Task<IEnumerable<Veiculo>> GetByPlacaForCliente(string placa, int clienteId)
        {
            return await VeiculosDoCliente(clienteId)
                .Where(v => v.PlacaVeiculo.Contains(placa))
                .ToListAsync();
        }

        public async Task<IEnumerable<Veiculo>> GetByPlacaForOficina(string placa, int oficinaId)
        {
            return await VeiculosDaOficina(oficinaId, asNoTracking: true)
                .Where(v => v.PlacaVeiculo.Contains(placa))
                .ToListAsync();
        }

        public async Task<IEnumerable<Veiculo>> GetByTipo(string tipo)
        {
            return await VeiculosComDetalhes()
                .Where(v => v.ModeloVeiculo.Contains(tipo))
                .ToListAsync();
        }

        public async Task<IEnumerable<Veiculo>> GetByTipoForCliente(string tipo, int clienteId)
        {
            return await VeiculosDoCliente(clienteId)
                .Where(v => v.ModeloVeiculo.Contains(tipo))
                .ToListAsync();
        }

        public async Task<IEnumerable<Veiculo>> GetByTipoForOficina(string tipo, int oficinaId)
        {
            return await VeiculosDaOficina(oficinaId, asNoTracking: true)
                .Where(v => v.ModeloVeiculo.Contains(tipo))
                .ToListAsync();
        }

        public async Task<IEnumerable<Veiculo>> GetByCliente(int clienteId)
        {
            return await VeiculosDoCliente(clienteId).ToListAsync();
        }

        public async Task<IEnumerable<Veiculo>> GetByOficina(int oficinaId)
        {
            return await VeiculosDaOficina(oficinaId, asNoTracking: true).ToListAsync();
        }

        public async Task<Veiculo?> GetById(int id)
        {
            return await VeiculosComDetalhes()
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<Veiculo?> GetByIdWithImagens(int id)
        {
            return await VeiculosComDetalhes()
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<Veiculo?> GetByIdForCliente(int id, int clienteId)
        {
            return await VeiculosDoCliente(clienteId)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<Veiculo?> GetByIdForOficina(int id, int oficinaId)
        {
            return await VeiculosDaOficina(oficinaId)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<Veiculo?> GetByIdForOficinaWithImagens(int id, int oficinaId)
        {
            return await _context.Veiculos
                .Include(v => v.Imagens)
                .Where(v =>
                    v.Id == id &&
                    v.Cliente.Situacao == SIGO.Objects.Enums.Situacao.ATIVO &&
                    v.Cliente.ClienteOficinas.Any(co =>
                        co.OficinaId == oficinaId &&
                        co.Ativo))
                .FirstOrDefaultAsync();
        }

        public async Task UpdateVeiculo(Veiculo veiculo)
        {
            // Atualiza só os campos necessários
            var existing = await _context.Veiculos.FindAsync(veiculo.Id);

            if (existing == null)
                throw new KeyNotFoundException($"Veículo com id {veiculo.Id} não encontrado.");

            // Atualiza os campos desejados
            existing.PlacaVeiculo = veiculo.PlacaVeiculo;
            existing.AnoFab = veiculo.AnoFab;
            existing.Id = veiculo.Id; // mantém relacionamento correto

            _context.Veiculos.Update(existing);
            await _context.SaveChangesAsync();
        }

        private IQueryable<Veiculo> VeiculosDoCliente(int clienteId)
        {
            return VeiculosComDetalhes()
                .Where(v => v.ClienteId == clienteId);
        }

        private IQueryable<Veiculo> VeiculosDaOficina(int oficinaId, bool asNoTracking = false)
        {
            var query = _context.Veiculos
                .AsSplitQuery()
                .Include(v => v.Cliente)
                .Include(v => v.Imagens)
                .Include(v => v.RegistroServicos.Where(registro =>
                    registro.OficinaId == oficinaId))
                    .ThenInclude(registro => registro.Servico)
                .Include(v => v.RegistroServicos.Where(registro =>
                    registro.OficinaId == oficinaId))
                    .ThenInclude(registro => registro.PecasSubstituidas)
                .Include(v => v.Pedidos.Where(pedido => pedido.idOficina == oficinaId))
                    .ThenInclude(pedido => pedido.Pedido_Servicos)
                    .ThenInclude(pedidoServico => pedidoServico.Servico)
                .Include(v => v.Pedidos.Where(pedido => pedido.idOficina == oficinaId))
                    .ThenInclude(pedido => pedido.Pedido_Pecas)
                    .ThenInclude(pedidoPeca => pedidoPeca.Peca)
                .Where(v =>
                    v.Cliente.Situacao == SIGO.Objects.Enums.Situacao.ATIVO &&
                    v.Cliente.ClienteOficinas.Any(co =>
                        co.OficinaId == oficinaId &&
                        co.Ativo));

            return asNoTracking
                ? query.AsNoTracking()
                : query;
        }

        private IQueryable<Veiculo> VeiculosComDetalhes()
        {
            return _context.Veiculos
                .AsSplitQuery()
                .Include(v => v.Cliente)
                .Include(v => v.Imagens)
                .Include(v => v.RegistroServicos).ThenInclude(r => r.Servico)
                .Include(v => v.RegistroServicos).ThenInclude(r => r.PecasSubstituidas)
                .Include(v => v.Pedidos).ThenInclude(p => p.Pedido_Servicos).ThenInclude(ps => ps.Servico)
                .Include(v => v.Pedidos).ThenInclude(p => p.Pedido_Pecas).ThenInclude(pp => pp.Peca);
        }
    }
}
