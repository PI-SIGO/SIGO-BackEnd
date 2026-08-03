using SIGO.Objects.Models;

namespace SIGO.Data.Interfaces
{
    public interface IClienteOficinaRepository
    {
        Task<bool> ExistsAsync(int oficinaId, int clienteId);
        Task<ClienteOficina?> GetAsync(
            int oficinaId,
            int clienteId,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ClienteOficina>> GetByClienteAsync(
            int clienteId,
            CancellationToken cancellationToken = default);
        Task<ClienteOficina> AddOrActivateAsync(
            int oficinaId,
            int clienteId,
            CancellationToken cancellationToken = default);
        Task<bool> DeactivateAsync(
            int oficinaId,
            int clienteId,
            DateTime updatedAt,
            CancellationToken cancellationToken = default);
        Task<bool> DeactivateByOficinaAsync(
            int oficinaId,
            int clienteId,
            DateTime updatedAt,
            CancellationToken cancellationToken = default);
    }
}
