using Microsoft.EntityFrameworkCore;
using SIGO.Data.Interfaces;
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
                .ToListAsync();
        }

        public async Task<Cliente?> GetByIdWithDetails(int id)
        {
            return await _context.Clientes
                .Include(c => c.Telefones)
                .Include(c => c.Veiculos)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Cliente>> GetByOficina(int oficinaId)
        {
            return await ClientesComDetalhes()
                .Where(c => c.ClienteOficinas.Any(co => co.OficinaId == oficinaId && co.Ativo))
                .ToListAsync();
        }

        public async Task<Cliente?> GetByIdWithDetailsForOficina(int id, int oficinaId)
        {
            return await ClientesComDetalhes()
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    c.ClienteOficinas.Any(co => co.OficinaId == oficinaId && co.Ativo));
        }

        public async Task<IEnumerable<Cliente>> GetByNameWithDetails(string nome)
        {
            return await _context.Clientes
                .Include(c => c.Telefones)
                .Include(c => c.Veiculos)
                .Where(c => c.Nome.Contains(nome))
                .ToListAsync();
        }

        public async Task<IEnumerable<Cliente>> GetByNameWithDetailsForOficina(string nome, int oficinaId)
        {
            return await ClientesComDetalhes()
                .Where(c =>
                    c.Nome.Contains(nome) &&
                    c.ClienteOficinas.Any(co => co.OficinaId == oficinaId && co.Ativo))
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

        public async Task<Cliente?> GetByEmail(string email)
        {
            var emailNormalizado = NormalizeEmail(email);

            return await _context.Clientes
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.Email != null &&
                    c.Email.Trim().ToLower() == emailNormalizado);
        }

        public async Task UpdatePasswordHash(int id, string passwordHash)
        {
            await _context.Clientes
                .Where(c => c.Id == id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.Senha, passwordHash));
        }

        public async Task<bool> ExistsInOficina(int clienteId, int oficinaId)
        {
            return await _context.ClienteOficinas
                .AnyAsync(co => co.ClienteId == clienteId && co.OficinaId == oficinaId && co.Ativo);
        }

        public async Task<bool> AllowsFieldInOficina(int clienteId, int oficinaId, string campo)
        {
            var campoJson = $"\"{campo}\"";
            return await _context.ClienteOficinas
                .AnyAsync(co =>
                    co.ClienteId == clienteId &&
                    co.OficinaId == oficinaId &&
                    co.Ativo &&
                    co.DadosPermitidos.Contains(campoJson));
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

        public async Task<IReadOnlyList<Cliente>> GetByCpfCnpjOrEmail(string cpfCnpj, string email)
        {
            var documentoNormalizado = SomenteDigitos(cpfCnpj);
            var emailNormalizado = email.Trim().ToLowerInvariant();

            return await _context.Clientes
                .Where(c =>
                    (c.Cpf_Cnpj != null &&
                     c.Cpf_Cnpj.Replace(".", "").Replace("-", "").Replace("/", "") == documentoNormalizado) ||
                    (c.Email != null &&
                     c.Email.Trim().ToLower() == emailNormalizado))
                .ToListAsync();
        }

        private static string SomenteDigitos(string valor) =>
            new(valor.Where(char.IsDigit).ToArray());

        private static string NormalizeEmail(string email) =>
            email.Trim().ToLowerInvariant();

        private IQueryable<Cliente> ClientesComDetalhes()
        {
            return _context.Clientes
                .Include(c => c.Telefones)
                .Include(c => c.Veiculos)
                .Include(c => c.ClienteOficinas);
        }
    }
}
