using Microsoft.EntityFrameworkCore;
using SIGO.Data.Interfaces;
using SIGO.Objects.Enums;
using SIGO.Objects.Models;
using SIGO.Validation;
using System.Linq;

namespace SIGO.Data.Repositories
{
    public class OficinaRepository : GenericRepository<Oficina>, IOficinaRepository
    {
        private readonly AppDbContext _context;

        public OficinaRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public override async Task<IEnumerable<Oficina>> Get()
        {
            return await _context.Oficinas
                .AsNoTracking()
                .Where(oficina => oficina.Situacao == Situacao.ATIVO)
                .ToListAsync();
        }

        public async Task<Oficina?> GetByEmail(string email)
        {
            var emailNormalizado = EmailNormalizer.Normalize(email);

            return await _context.Oficinas
                .AsNoTracking()
                .FirstOrDefaultAsync(o =>
                    o.Email != null &&
                    o.Email.Trim().ToLower() == emailNormalizado);
        }

        public async Task<bool> ExistsByEmail(string email, int? ignoreId = null)
        {
            var emailNormalizado = EmailNormalizer.Normalize(email);

            return await _context.Oficinas.AnyAsync(oficina =>
                oficina.Email != null &&
                oficina.Email.Trim().ToLower() == emailNormalizado &&
                (!ignoreId.HasValue || oficina.Id != ignoreId.Value));
        }

        public async Task<Oficina?> GetActiveByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await _context.Oficinas
                .FirstOrDefaultAsync(
                    oficina =>
                        oficina.Id == id &&
                        oficina.Situacao == Situacao.ATIVO,
                    cancellationToken);
        }

        public async Task<bool> IsActiveAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await _context.Oficinas.AnyAsync(
                oficina =>
                    oficina.Id == id &&
                    oficina.Situacao == Situacao.ATIVO,
                cancellationToken);
        }

        public async Task UpdatePasswordHash(int id, string passwordHash)
        {
            await _context.Oficinas
                .Where(o => o.Id == id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(o => o.Senha, passwordHash));
        }

        public async Task<bool> DeactivateAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var affectedRows = await _context.Oficinas
                .Where(oficina => oficina.Id == id)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        oficina => oficina.Situacao,
                        Situacao.INATIVO),
                    cancellationToken);

            return affectedRows > 0;
        }

        public async Task<IEnumerable<Oficina>> GetByName(string nomeOficina)
        {
            return await _context.Oficinas
                .AsNoTracking()
                .Where(oficina =>
                    oficina.Situacao == Situacao.ATIVO &&
                    oficina.Nome.Contains(nomeOficina))
                .ToListAsync();
        }

        public async Task<bool> ExistsByCnpj(string cnpj, int? ignoreId = null)
        {
            var cnpjNormalizado = SomenteDigitos(cnpj);

            return await _context.Oficinas
                .AnyAsync(o =>
                    o.CNPJ != null &&
                    o.CNPJ.Replace(".", "").Replace("-", "").Replace("/", "") == cnpjNormalizado &&
                    (!ignoreId.HasValue || o.Id != ignoreId.Value));
        }

        private static string SomenteDigitos(string valor) =>
            new(valor.Where(char.IsDigit).ToArray());

    }
}
