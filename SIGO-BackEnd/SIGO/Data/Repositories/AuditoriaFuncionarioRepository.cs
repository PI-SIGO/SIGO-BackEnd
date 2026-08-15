using Microsoft.EntityFrameworkCore;
using SIGO.Data.Interfaces;
using SIGO.Objects.Models;

namespace SIGO.Data.Repositories
{
    public class AuditoriaFuncionarioRepository
        : IAuditoriaFuncionarioRepository
    {
        private readonly AppDbContext _context;

        public AuditoriaFuncionarioRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task Add(AuditoriaFuncionario auditoria)
        {
            await _context.AuditoriasFuncionarios.AddAsync(auditoria);

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<AuditoriaFuncionario>> Get(
            int? funcionarioId = null,
            string? acao = null,
            string? entidade = null,
            DateTime? inicio = null,
            DateTime? fim = null,
            int? oficinaId = null)
        {
            var query = _context.AuditoriasFuncionarios
                .AsNoTracking()
                .AsQueryable();

            if (oficinaId.HasValue)
            {
                query = query.Where(auditoria =>
                    _context.Funcionarios.Any(funcionario =>
                        funcionario.Id == auditoria.FuncionarioId &&
                        funcionario.IdOficina == oficinaId.Value));
            }

            if (funcionarioId.HasValue)
            {
                query = query.Where(
                    a => a.FuncionarioId == funcionarioId.Value);
            }

            if (!string.IsNullOrWhiteSpace(acao))
            {
                query = query.Where(
                    a => a.Acao == acao.ToUpper());
            }

            if (!string.IsNullOrWhiteSpace(entidade))
            {
                query = query.Where(
                    a => a.Entidade == entidade);
            }

            if (inicio.HasValue)
            {
                query = query.Where(
                    a => a.DataHora >= inicio.Value);
            }

            if (fim.HasValue)
            {
                query = query.Where(
                    a => a.DataHora <= fim.Value);
            }

            return await query
                .OrderByDescending(a => a.DataHora)
                .ToListAsync();
        }
    }
}
