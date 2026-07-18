using SIGO.Objects.Models;

namespace SIGO.Data.Interfaces
{
    public interface IClienteContaRepository
    {
        Task<ClienteConta?> GetByEmailAsync(string emailNormalizado, CancellationToken cancellationToken = default);
        Task<ClienteConta?> GetByClienteIdAsync(int clienteId, CancellationToken cancellationToken = default);
        Task<bool> EmailInUseByOtherClienteAsync(
            string emailNormalizado,
            int? clienteId,
            CancellationToken cancellationToken = default);
        Task<int?> GetActiveTokenVersionAsync(int clienteId, CancellationToken cancellationToken = default);
        Task AddAsync(ClienteConta conta, CancellationToken cancellationToken = default);
        Task<bool> TryUpdatePasswordAsync(
            int clienteId,
            string expectedPasswordHash,
            int expectedTokenVersion,
            string newPasswordHash,
            DateTime updatedAt,
            CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
