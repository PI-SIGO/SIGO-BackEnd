using Microsoft.EntityFrameworkCore;
using SIGO.Data.Interfaces;
using SIGO.Objects.Models;
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

        public async Task<Oficina?> GetByEmail(string email)
        {
            var emailNormalizado = NormalizeEmail(email);

            return await _context.Oficinas
                .AsNoTracking()
                .FirstOrDefaultAsync(o =>
                    o.Email != null &&
                    o.Email.Trim().ToLower() == emailNormalizado);
        }

        public async Task UpdatePasswordHash(int id, string passwordHash)
        {
            await _context.Oficinas
                .Where(o => o.Id == id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(o => o.Senha, passwordHash));
        }

        public async Task<IEnumerable<Oficina>> GetByName(string nomeOficina)
        {
            return await _context.Oficinas
                .Where(m => m.Nome.Contains(nomeOficina))
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

        private static string NormalizeEmail(string email) =>
            email.Trim().ToLowerInvariant();
    }
}
