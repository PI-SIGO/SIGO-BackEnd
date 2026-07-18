using Microsoft.EntityFrameworkCore;
using SIGO.Data.Interfaces;
using SIGO.Objects.Enums;
using SIGO.Objects.Models;
using SIGO.Security;
using SIGO.Validation;
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

        public override async Task<IEnumerable<Funcionario>> Get()
        {
            return await ActiveFuncionarios()
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Funcionario?> GetByEmail(string email)
        {
            var emailNormalizado = EmailNormalizer.Normalize(email);

            return await _context.Funcionarios
                .Include(funcionario => funcionario.Oficina)
                .AsNoTracking()
                .FirstOrDefaultAsync(f =>
                    f.Email != null &&
                    f.Email.Trim().ToLower() == emailNormalizado);
        }

        public async Task<bool> ExistsByEmail(string email, int? ignoreId = null)
        {
            var emailNormalizado = EmailNormalizer.Normalize(email);

            return await _context.Funcionarios.AnyAsync(funcionario =>
                funcionario.Email != null &&
                funcionario.Email.Trim().ToLower() == emailNormalizado &&
                (!ignoreId.HasValue || funcionario.Id != ignoreId.Value));
        }

        public async Task<Funcionario?> GetActiveByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await ActiveFuncionarios()
                .FirstOrDefaultAsync(
                    funcionario => funcionario.Id == id,
                    cancellationToken);
        }

        public async Task<bool> IsActiveAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await ActiveFuncionarios().AnyAsync(
                funcionario => funcionario.Id == id,
                cancellationToken);
        }

        public async Task UpdatePasswordHash(int id, string passwordHash)
        {
            await _context.Funcionarios
                .Where(f => f.Id == id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(f => f.Senha, passwordHash));
        }

        public async Task<bool> DeactivateAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var affectedRows = await _context.Funcionarios
                .Where(funcionario => funcionario.Id == id)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        funcionario => funcionario.Situacao,
                        Situacao.INATIVO),
                    cancellationToken);

            return affectedRows > 0;
        }

        public async Task<IEnumerable<Funcionario>> GetFuncionarioByNome(string nome)
        {
            return await ActiveFuncionarios()
                .AsNoTracking()
                .Where(funcionario => funcionario.Nome.Contains(nome))
                .ToListAsync();
        }

        public async Task<IEnumerable<Funcionario>> GetByOficina(int oficinaId)
        {
            return await ActiveFuncionarios()
                .AsNoTracking()
                .Where(funcionario => funcionario.IdOficina == oficinaId)
                .ToListAsync();
        }

        public async Task<Funcionario?> GetByIdForOficina(int id, int oficinaId)
        {
            return await ActiveFuncionarios()
                .AsNoTracking()
                .FirstOrDefaultAsync(funcionario =>
                    funcionario.Id == id &&
                    funcionario.IdOficina == oficinaId);
        }

        public async Task<IEnumerable<Funcionario>> GetFuncionarioByNomeForOficina(string nome, int oficinaId)
        {
            return await ActiveFuncionarios()
                .AsNoTracking()
                .Where(funcionario =>
                    funcionario.IdOficina == oficinaId &&
                    funcionario.Nome.Contains(nome))
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
            return await ActiveFuncionarios()
                .AnyAsync(funcionario =>
                    funcionario.Id == funcionarioId &&
                    funcionario.IdOficina == oficinaId);
        }

        private IQueryable<Funcionario> ActiveFuncionarios()
        {
            return _context.Funcionarios.Where(funcionario =>
                funcionario.Situacao == Situacao.ATIVO &&
                (funcionario.Role == SystemRoles.Admin ||
                 (funcionario.Oficina != null &&
                  funcionario.Oficina.Situacao == Situacao.ATIVO)));
        }

        private static string SomenteDigitos(string valor) =>
            new(valor.Where(char.IsDigit).ToArray());

    }
}
