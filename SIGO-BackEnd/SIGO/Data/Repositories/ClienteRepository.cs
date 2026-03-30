using Microsoft.EntityFrameworkCore;
using SIGO.Data.Interfaces;
using SIGO.Objects.Contracts;
using SIGO.Objects.Models;
using System.Linq;

namespace SIGO.Data.Repositories
{
    public class ClienteRepository : GenericRepository<Cliente>, IClienteRepository
    {
        private readonly AppDbContext _context;

        public ClienteRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }
        public override async Task<IEnumerable<Cliente>> Get()
        {
            return await _context.Clientes
                .Include(c => c.Telefones)
                .Include(c => c.Veiculos)
                    .ThenInclude(v => v.Cor)
                .ToListAsync();
        }

        public async Task<Cliente?> GetByIdWithDetails(int id)
        {
            return await _context.Clientes
                .Include(c => c.Telefones)
                .Include(c => c.Veiculos)
                    .ThenInclude(v => v.Cor)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Cliente>> GetByNameWithDetails(string nome)
        {
            return await _context.Clientes
                .Include(c => c.Telefones)
                .Include(c => c.Veiculos)
                    .ThenInclude(v => v.Cor)
                .Where(c => c.Nome.Contains(nome))
                .ToListAsync();
        }

        public async Task<Cliente?> GetById(int id)
        {
            return await _context.Clientes
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Cliente> Add(Cliente cliente)
        {
            await _context.Clientes.AddAsync(cliente);
            await _context.SaveChangesAsync();
            return cliente;
        }

        public async Task<Cliente> Login(Login login)
        {
            return await _context.Clientes.AsNoTracking().FirstOrDefaultAsync(p => p.Email == login.Email && p.Senha == login.Password);
        }
        public async Task<bool> ExistsByCpfCnpj(string cpfCnpj, int? ignoreId = null)
        {
            var documentoNormalizado = SomenteDigitos(cpfCnpj);

            return await _context.Clientes
                .AnyAsync(c =>
                    c.Cpf_Cnpj != null &&
                    c.Cpf_Cnpj.Replace(".", "").Replace("-", "").Replace("/", "") == documentoNormalizado &&
                    (!ignoreId.HasValue || c.Id != ignoreId.Value));
        }

        public async Task<bool> ExistsByNome(string nome, int? ignoreId = null)
        {
            var nomeNormalizado = nome.Trim().ToLowerInvariant();

            return await _context.Clientes
                .AnyAsync(c =>
                    c.Nome != null &&
                    c.Nome.Trim().ToLower() == nomeNormalizado &&
                    (!ignoreId.HasValue || c.Id != ignoreId.Value));
        }

        public async Task<bool> ExistsByEmail(string email, int? ignoreId = null)
        {
            var emailNormalizado = email.Trim().ToLowerInvariant();

            return await _context.Clientes
                .AnyAsync(c =>
                    c.Email != null &&
                    c.Email.Trim().ToLower() == emailNormalizado &&
                    (!ignoreId.HasValue || c.Id != ignoreId.Value));
        }

        private static string SomenteDigitos(string valor) =>
            new(valor.Where(char.IsDigit).ToArray());
    }
}
