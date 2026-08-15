using System.Data;
using Microsoft.EntityFrameworkCore;
using SIGO.Data.Interfaces;
using SIGO.Objects.Enums;
using SIGO.Objects.Models;

namespace SIGO.Data.Repositories
{
    public sealed class ClienteIdentityRepository : IClienteIdentityRepository
    {
        private readonly AppDbContext _context;

        public ClienteIdentityRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            try
            {
                var result = await operation(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public Task<Cliente?> GetClienteByCpfCnpjAsync(
            string documentoNormalizado,
            CancellationToken cancellationToken = default)
        {
            return _context.Clientes
                .Include(cliente => cliente.Conta)
                .Include(cliente => cliente.Telefones)
                .FirstOrDefaultAsync(
                    cliente => cliente.Cpf_Cnpj == documentoNormalizado,
                    cancellationToken);
        }

        public Task<ClienteContato?> GetContatoAsync(
            int clienteId,
            TipoContatoCliente tipo,
            string valorNormalizado,
            CancellationToken cancellationToken = default)
        {
            return _context.ClienteContatos.FirstOrDefaultAsync(
                contato => contato.ClienteId == clienteId &&
                           contato.Tipo == tipo &&
                           contato.ValorNormalizado == valorNormalizado,
                cancellationToken);
        }

        public async Task AddClienteAsync(
            Cliente cliente,
            CancellationToken cancellationToken = default)
        {
            await _context.Clientes.AddAsync(cliente, cancellationToken);
        }

        public async Task AddContatoAsync(
            ClienteContato contato,
            CancellationToken cancellationToken = default)
        {
            await _context.ClienteContatos.AddAsync(contato, cancellationToken);
        }

        public async Task AddAuditoriaAsync(
            AuditoriaSeguranca auditoria,
            CancellationToken cancellationToken = default)
        {
            await _context.AuditoriasSeguranca.AddAsync(auditoria, cancellationToken);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
