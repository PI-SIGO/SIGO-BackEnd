using Microsoft.EntityFrameworkCore;
using SIGO.Data.Interfaces;
using SIGO.Objects.Models;
using System.Linq;

namespace SIGO.Data.Repositories
{
    public class FuncionarioRepository : GenericRepository<Funcionario>, IFuncionarioRepository
    {
        private readonly AppDbContext _context;

        public FuncionarioRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Funcionario?> GetByEmail(string email)
        {
            var emailNormalizado = NormalizeEmail(email);

            return await _context.Funcionarios
                .AsNoTracking()
                .FirstOrDefaultAsync(f =>
                    f.Email != null &&
                    f.Email.Trim().ToLower() == emailNormalizado);
        }

        public async Task UpdatePasswordHash(int id, string passwordHash)
        {
            await _context.Funcionarios
                .Where(f => f.Id == id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(f => f.Senha, passwordHash));
        }

        public async Task<IEnumerable<Funcionario>> GetFuncionarioByNome(string nome)
        {
            return await _context.Funcionarios
            .Where(f => f.Nome.Contains(nome))
            .ToListAsync();
        }

        public async Task<IEnumerable<Funcionario>> GetByOficina(int oficinaId)
        {
            return await _context.Funcionarios
                .Where(f => f.IdOficina == oficinaId)
                .ToListAsync();
        }

        public async Task<Funcionario?> GetByIdForOficina(int id, int oficinaId)
        {
            return await _context.Funcionarios
                .FirstOrDefaultAsync(f => f.Id == id && f.IdOficina == oficinaId);
        }

        public async Task<IEnumerable<Funcionario>> GetFuncionarioByNomeForOficina(string nome, int oficinaId)
        {
            return await _context.Funcionarios
                .Where(f => f.IdOficina == oficinaId && f.Nome.Contains(nome))
                .ToListAsync();
        }

        public async Task<bool> ExistsByCpf(string cpf, int? ignoreId = null)
        {
            var cpfNormalizado = SomenteDigitos(cpf);

            return await _context.Funcionarios
                .AnyAsync(f =>
                    f.Cpf != null &&
                    f.Cpf.Replace(".", "").Replace("-", "") == cpfNormalizado &&
                    (!ignoreId.HasValue || f.Id != ignoreId.Value));
        }

        public async Task<bool> ExistsInOficina(int funcionarioId, int oficinaId)
        {
            return await _context.Funcionarios
                .AnyAsync(f => f.Id == funcionarioId && f.IdOficina == oficinaId);
        }

        private static string SomenteDigitos(string valor) =>
            new(valor.Where(char.IsDigit).ToArray());

        private static string NormalizeEmail(string email) =>
            email.Trim().ToLowerInvariant();
    }
}
