using Microsoft.EntityFrameworkCore;
using SIGO.Data.Interfaces;
using SIGO.Objects.Models;
using SIGO.Objects.Contracts;
using SIGO.Services.Interfaces;
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

        public async Task<Oficina?> Login(Login login)
        {
            return await _context.Oficinas.AsNoTracking().FirstOrDefaultAsync(o => o.Email == login.Email && o.Senha == login.Password);
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
    }
}

