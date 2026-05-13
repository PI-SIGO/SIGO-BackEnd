using SIGO.Objects.Models;

namespace SIGO.Data.Interfaces
{
    public interface IClienteRepository : IGenericRepository<Cliente>
    {
        Task<IEnumerable<Cliente>> GetByNameWithDetails(string nome);
        Task<IEnumerable<Cliente>> GetByOficina(int oficinaId);
        Task<IEnumerable<Cliente>> GetByNameWithDetailsForOficina(string nome, int oficinaId);
        Task<Cliente?> GetByIdWithDetails(int id);
        Task<Cliente?> GetByIdWithDetailsForOficina(int id, int oficinaId);
        Task<Cliente> Add(Cliente cliente);
        Task<Cliente?> GetByEmail(string email);
        Task UpdatePasswordHash(int id, string passwordHash);
        Task<bool> ExistsInOficina(int clienteId, int oficinaId);
        Task<bool> AllowsFieldInOficina(int clienteId, int oficinaId, string campo);
        Task<bool> ExistsByCpfCnpj(string cpfCnpj, int? ignoreId = null);
        Task<bool> ExistsByNome(string nome, int? ignoreId = null);
        Task<bool> ExistsByEmail(string email, int? ignoreId = null);
        Task<IReadOnlyList<Cliente>> GetByCpfCnpjOrEmail(string cpfCnpj, string email);
    }
}
