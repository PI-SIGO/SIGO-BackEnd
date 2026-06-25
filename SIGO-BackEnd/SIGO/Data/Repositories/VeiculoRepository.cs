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
            return await VeiculosDaOficina(oficinaId)
                .Where(v => v.PlacaVeiculo.Contains(placa))
                .ToListAsync();
        }

        public async Task<IEnumerable<Veiculo>> GetByTipo(string tipo)
        {
            return await VeiculosComDetalhes()
                .Where(v => v.TipoVeiculo.Contains(tipo))
                .ToListAsync();
        }

        public async Task<IEnumerable<Veiculo>> GetByTipoForCliente(string tipo, int clienteId)
        {
            return await VeiculosDoCliente(clienteId)
                .Where(v => v.TipoVeiculo.Contains(tipo))
                .ToListAsync();
        }

        public async Task<IEnumerable<Veiculo>> GetByTipoForOficina(string tipo, int oficinaId)
        {
            return await VeiculosDaOficina(oficinaId)
                .Where(v => v.TipoVeiculo.Contains(tipo))
                .ToListAsync();
        }

        public async Task<IEnumerable<Veiculo>> GetByCliente(int clienteId)
        {
            return await VeiculosDoCliente(clienteId).ToListAsync();
        }

        public async Task<IEnumerable<Veiculo>> GetByOficina(int oficinaId)
        {
            return await VeiculosDaOficina(oficinaId).ToListAsync();
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

        private IQueryable<Veiculo> VeiculosDaOficina(int oficinaId)
        {
            return VeiculosComDetalhes()
                .Where(v => v.Cliente.ClienteOficinas.Any(co =>
                    co.OficinaId == oficinaId &&
                    co.Ativo));
        }

        private IQueryable<Veiculo> VeiculosComDetalhes()
        {
            return _context.Veiculos
                .AsSplitQuery()
                .Include(v => v.Cliente)
                .Include(v => v.Marcas)
                .Include(v => v.Imagens)
                .Include(v => v.RegistroServicos).ThenInclude(r => r.Servico)
                .Include(v => v.RegistroServicos).ThenInclude(r => r.PecasSubstituidas)
                .Include(v => v.Pedidos).ThenInclude(p => p.Pedido_Servicos).ThenInclude(ps => ps.Servico)
                .Include(v => v.Pedidos).ThenInclude(p => p.Pedido_Pecas).ThenInclude(pp => pp.Peca);
        }
    }
}
