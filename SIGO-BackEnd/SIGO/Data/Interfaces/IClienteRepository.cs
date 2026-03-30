using SIGO.Objects.Contracts;
using SIGO.Objects.Models;

namespace SIGO.Data.Interfaces
{
    public interface IClienteRepository : IGenericRepository<Cliente>
    {
        Task<IEnumerable<Cliente>> GetByNameWithDetails(string nome);
        Task<Cliente?> GetByIdWithDetails(int id);
        Task<Cliente> Add(Cliente cliente);
        Task<Cliente> Login(Login login);
        Task<bool> ExistsByCpfCnpj(string cpfCnpj, int? ignoreId = null);
        Task<bool> ExistsByNome(string nome, int? ignoreId = null);
        Task<bool> ExistsByEmail(string email, int? ignoreId = null);
    }
}
