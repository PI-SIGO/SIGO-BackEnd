using SIGO.Objects.Models;

namespace SIGO.Data.Interfaces
{
    public interface IOficinaRepository : IGenericRepository<Oficina>
    {
        Task<IEnumerable<Oficina>> GetByName(string nomeMarca);
        Task<bool> ExistsByCnpj(string cnpj, int? ignoreId = null);
        Task<bool> ExistsByEmail(string email, int? ignoreId = null);
        Task<Oficina?> GetByEmail(string email);
        Task<Oficina?> GetActiveByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> IsActiveAsync(int id, CancellationToken cancellationToken = default);
        Task UpdatePasswordHash(int id, string passwordHash);
        Task<bool> DeactivateAsync(int id, CancellationToken cancellationToken = default);
    }
}
