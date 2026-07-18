using Microsoft.EntityFrameworkCore;
using SIGO.Data.Interfaces;
using SIGO.Objects.Enums;
using SIGO.Objects.Models;

namespace SIGO.Data.Repositories
{
    public sealed class ClienteContaRepository : IClienteContaRepository
    {
        private readonly AppDbContext _context;

        public ClienteContaRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<ClienteConta?> GetByEmailAsync(
            string emailNormalizado,
            CancellationToken cancellationToken = default)
        {
            return _context.ClienteContas
                .AsNoTracking()
                .Include(conta => conta.Cliente)
                .FirstOrDefaultAsync(
                    conta => conta.EmailNormalizado == emailNormalizado,
                    cancellationToken);
        }

        public Task<ClienteConta?> GetByClienteIdAsync(
            int clienteId,
            CancellationToken cancellationToken = default)
        {
            return _context.ClienteContas
                .Include(conta => conta.Cliente)
                .FirstOrDefaultAsync(conta => conta.ClienteId == clienteId, cancellationToken);
        }

        public Task<bool> EmailInUseByOtherClienteAsync(
            string emailNormalizado,
            int? clienteId,
            CancellationToken cancellationToken = default)
        {
            return _context.ClienteContas.AnyAsync(
                conta => conta.EmailNormalizado == emailNormalizado &&
                         (!clienteId.HasValue || conta.ClienteId != clienteId.Value),
                cancellationToken);
        }

        public Task<int?> GetActiveTokenVersionAsync(
            int clienteId,
            CancellationToken cancellationToken = default)
        {
            return _context.ClienteContas
                .AsNoTracking()
                .Where(conta =>
                    conta.ClienteId == clienteId &&
                    conta.Status == EstadoClienteConta.Active &&
                    conta.Cliente.Situacao == SIGO.Objects.Enums.Situacao.ATIVO)
                .Select(conta => (int?)conta.TokenVersion)
                .SingleOrDefaultAsync(cancellationToken);
        }

        public async Task AddAsync(ClienteConta conta, CancellationToken cancellationToken = default)
        {
            await _context.ClienteContas.AddAsync(conta, cancellationToken);
        }

        public async Task<bool> TryUpdatePasswordAsync(
            int clienteId,
            string expectedPasswordHash,
            int expectedTokenVersion,
            string newPasswordHash,
            DateTime updatedAt,
            CancellationToken cancellationToken = default)
        {
            var updated = await _context.ClienteContas
                .Where(conta =>
                    conta.ClienteId == clienteId &&
                    conta.Status == EstadoClienteConta.Active &&
                    conta.PasswordHash == expectedPasswordHash &&
                    conta.TokenVersion == expectedTokenVersion)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(conta => conta.PasswordHash, newPasswordHash)
                        .SetProperty(conta => conta.TokenVersion, conta => conta.TokenVersion + 1)
                        .SetProperty(conta => conta.UpdatedAt, updatedAt),
                    cancellationToken);

            return updated == 1;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
