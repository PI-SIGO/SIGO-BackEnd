using Microsoft.EntityFrameworkCore;
using SIGO.Data.Interfaces;
using SIGO.Objects.Models;
using SIGO.Objects.Enums;
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
                .Where(c => c.Situacao == Situacao.ATIVO)
                .ToListAsync();
        }

        public async Task<Cliente?> GetByIdWithDetails(int id)
        {
            return await _context.Clientes
                .Include(c => c.Telefones)
                .Include(c => c.Veiculos)
                .FirstOrDefaultAsync(c => c.Id == id && c.Situacao == Situacao.ATIVO);
        }

        public async Task<IEnumerable<Cliente>> GetByOficina(int oficinaId)
        {
            return await ClientesComDetalhes()
                .Where(c => c.ClienteOficinas.Any(co =>
                    co.OficinaId == oficinaId && co.Ativo))
                .Where(c => c.Situacao == Situacao.ATIVO)
                .ToListAsync();
        }

        public async Task<Cliente?> GetByIdWithDetailsForOficina(int id, int oficinaId)
        {
            return await ClientesComDetalhes()
                .FirstOrDefaultAsync(c =>
                    c.Id == id && c.Situacao == Situacao.ATIVO &&
                    c.ClienteOficinas.Any(co =>
                        co.OficinaId == oficinaId && co.Ativo));
        }

        public async Task<IEnumerable<Cliente>> GetByNameWithDetails(string nome)
        {
            return await _context.Clientes
                .Include(c => c.Telefones)
                .Include(c => c.Veiculos)
                .Where(c => c.Situacao == Situacao.ATIVO && c.Nome.Contains(nome))
                .ToListAsync();
        }

        public async Task<IEnumerable<Cliente>> GetByNameWithDetailsForOficina(string nome, int oficinaId)
        {
            return await ClientesComDetalhes()
                .Where(c =>
                    c.Situacao == Situacao.ATIVO && c.Nome.Contains(nome) &&
                    c.ClienteOficinas.Any(co =>
                        co.OficinaId == oficinaId && co.Ativo))
                .ToListAsync();
        }

        public async Task<Cliente?> GetById(int id)
        {
            return await _context.Clientes
                .FirstOrDefaultAsync(c => c.Id == id && c.Situacao == Situacao.ATIVO);
        }

        public async Task<Cliente> Add(Cliente cliente)
        {
            await _context.Clientes.AddAsync(cliente);
            await _context.SaveChangesAsync();
            return cliente;
        }

        public async Task<bool> ExistsInOficina(int clienteId, int oficinaId)
        {
            return await _context.ClienteOficinas
                .AnyAsync(co =>
                    co.ClienteId == clienteId &&
                    co.OficinaId == oficinaId &&
                    co.Ativo &&
                    co.Cliente.Situacao == Situacao.ATIVO);
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

        public Task<Cliente?> GetByCpfCnpjAsync(
            string cpfCnpj,
            CancellationToken cancellationToken = default)
        {
            var documentoNormalizado = SomenteDigitos(cpfCnpj);
            return _context.Clientes.FirstOrDefaultAsync(
                cliente => cliente.Cpf_Cnpj == documentoNormalizado,
                cancellationToken);
        }

        public async Task<bool> DeactivateAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var cliente = await _context.Clientes
                .Include(entity => entity.Conta)
                .Include(entity => entity.ClienteOficinas)
                .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);
            if (cliente is null || cliente.Situacao == Situacao.INATIVO)
                return false;

            cliente.Situacao = Situacao.INATIVO;
            var now = DateTime.UtcNow;
            if (cliente.Conta is not null)
            {
                cliente.Conta.Status = EstadoClienteConta.Blocked;
                cliente.Conta.TokenVersion++;
                cliente.Conta.UpdatedAt = now;
            }

            foreach (var relacionamento in cliente.ClienteOficinas.Where(item => item.Ativo))
            {
                relacionamento.Ativo = false;
                relacionamento.UpdatedAt = now;
                relacionamento.RevogadoEm = now;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private static string SomenteDigitos(string valor) =>
            new(valor.Where(char.IsDigit).ToArray());

        private IQueryable<Cliente> ClientesComDetalhes()
        {
            return _context.Clientes
                .AsNoTracking()
                .Include(c => c.Telefones)
                .Include(c => c.Veiculos)
                .Include(c => c.ClienteOficinas);
        }
    }
}
