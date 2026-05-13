using Microsoft.EntityFrameworkCore;
using SIGO.Data.Interfaces;
using SIGO.Objects.Models;

namespace SIGO.Data.Repositories
{
    public class ServicoRepository : GenericRepository<Servico>, IServicoRepository
    {
        private readonly AppDbContext _context;

        public ServicoRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }
        public override async Task<IEnumerable<Servico>> Get()
        {
            return await _context.Servicos
                .Include(s => s.Funcionario_Servicos)
                .ToListAsync();
        }


        public async Task<Servico?> GetByIdWithDetails(int id)
        {
            return await _context.Servicos
                .Include(s => s.Funcionario_Servicos)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Servico>> GetByNameWithDetails(string nome)
        {
            return await _context.Servicos
                .Include(s => s.Funcionario_Servicos)
                .Where(c => c.Nome.Contains(nome))
                .ToListAsync();
        }

        public async Task<IEnumerable<Servico>> GetByNameWithDetailsForOficina(string nome, int oficinaId)
        {
            return await ServicosDaOficina(oficinaId)
                .Where(c => c.Nome.Contains(nome))
                .ToListAsync();
        }

        public async Task<Servico?> GetById(int id)
        {
            return await _context.Servicos
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Servico?> GetByIdWithDetailsForOficina(int id, int oficinaId)
        {
            return await ServicosDaOficina(oficinaId)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Servico>> GetByOficina(int oficinaId)
        {
            return await ServicosDaOficina(oficinaId).ToListAsync();
        }

        public async Task<Servico?> GetByIdForOficina(int id, int oficinaId)
        {
            return await ServicosDaOficina(oficinaId)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Servico> Add(Servico servicos)
        {
            await _context.Servicos.AddAsync(servicos);
            await _context.SaveChangesAsync();
            return servicos;
        }

        private IQueryable<Servico> ServicosDaOficina(int oficinaId)
        {
            return _context.Servicos
                .Include(s => s.Funcionario_Servicos)
                .Where(s => s.IdOficina == oficinaId);
        }
    }
}
