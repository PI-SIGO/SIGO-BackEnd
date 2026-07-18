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
                .AsNoTracking()
                .ToListAsync();
        }


        public async Task<Servico?> GetByIdWithDetails(int id)
        {
            return await _context.Servicos
                .AsNoTracking()
                .Include(s => s.Funcionario_Servicos)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Servico>> GetByNameWithDetails(string nome)
        {
            return await _context.Servicos
                .AsNoTracking()
                .Include(s => s.Funcionario_Servicos)
                .Where(c => c.Nome.Contains(nome))
                .ToListAsync();
        }

        public async Task<IEnumerable<Servico>> GetByNameWithDetailsForOficina(string nome, int oficinaId)
        {
            return await ServicosDaOficinaComDetalhes(oficinaId)
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
            return await ServicosDaOficinaComDetalhes(oficinaId)
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

        public async Task<IReadOnlyList<Servico>> GetByIdsAsync(
            IReadOnlyCollection<int> ids,
            CancellationToken cancellationToken = default)
        {
            if (ids.Count == 0)
                return Array.Empty<Servico>();

            return await _context.Servicos
                .AsNoTracking()
                .Where(service => ids.Contains(service.Id))
                .ToListAsync(cancellationToken);
        }

        public async Task SaveWithEmployeesAsync(
            Servico servico,
            IReadOnlyCollection<Funcionario_Servico> employees,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var tracked = await _context.Servicos
                .Include(service => service.Funcionario_Servicos)
                .FirstOrDefaultAsync(service => service.Id == servico.Id, cancellationToken)
                ?? throw new KeyNotFoundException($"Servico com id {servico.Id} nao encontrado.");

            tracked.Nome = servico.Nome;
            tracked.Descricao = servico.Descricao;
            tracked.Valor = servico.Valor;
            tracked.Garantia = servico.Garantia;
            tracked.IdOficina = servico.IdOficina;

            var requestedByEmployeeId = employees.ToDictionary(employee => employee.IdFuncionario);
            foreach (var existing in tracked.Funcionario_Servicos.ToArray())
            {
                if (!requestedByEmployeeId.TryGetValue(existing.IdFuncionario, out var requested))
                {
                    _context.Set<Funcionario_Servico>().Remove(existing);
                    continue;
                }

                existing.TempoDec = requested.TempoDec;
            }

            var existingIds = tracked.Funcionario_Servicos
                .Select(employee => employee.IdFuncionario)
                .ToHashSet();
            foreach (var requested in employees.Where(employee => !existingIds.Contains(employee.IdFuncionario)))
            {
                tracked.Funcionario_Servicos.Add(new Funcionario_Servico
                {
                    IdFuncionario = requested.IdFuncionario,
                    IdServico = tracked.Id,
                    TempoDec = requested.TempoDec
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        private IQueryable<Servico> ServicosDaOficina(int oficinaId)
        {
            return _context.Servicos
                .AsNoTracking()
                .Where(s => s.IdOficina == oficinaId);
        }

        private IQueryable<Servico> ServicosDaOficinaComDetalhes(int oficinaId)
        {
            return ServicosDaOficina(oficinaId)
                .Include(s => s.Funcionario_Servicos);
        }
    }
}
