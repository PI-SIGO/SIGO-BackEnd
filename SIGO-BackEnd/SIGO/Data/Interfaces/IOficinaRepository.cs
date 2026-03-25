using SIGO.Objects.Models;

namespace SIGO.Data.Interfaces
{
    public interface IOficinaRepository : IGenericRepository<Oficina>
    {
        Task<IEnumerable<Oficina>> GetByName(string nomeMarca);
        Task<bool> ExistsByCnpj(string cnpj, int? ignoreId = null);
    }
}
