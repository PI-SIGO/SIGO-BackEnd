using SIGO.Objects.Models;

namespace SIGO.Data.Interfaces
{
    public interface IPecaRepository : IGenericRepository<Peca>
    {
        Task<IEnumerable<Peca>> GetByOficina(int oficinaId);
        Task<Peca?> GetByIdForOficina(int id, int oficinaId);
    }
}
