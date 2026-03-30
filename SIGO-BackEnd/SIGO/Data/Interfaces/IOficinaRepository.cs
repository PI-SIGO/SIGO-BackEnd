using SIGO.Objects.Models;
using SIGO.Objects.Contracts;

namespace SIGO.Data.Interfaces
{
    public interface IOficinaRepository : IGenericRepository<Oficina>
    {
        Task<IEnumerable<Oficina>> GetByName(string nomeMarca);
        Task<bool> ExistsByCnpj(string cnpj, int? ignoreId = null);
        Task<Oficina?> Login(Login login);
    }
}
