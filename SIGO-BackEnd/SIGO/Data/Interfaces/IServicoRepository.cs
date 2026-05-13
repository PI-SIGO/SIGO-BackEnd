using SIGO.Objects.Models;

namespace SIGO.Data.Interfaces
{
    public interface IServicoRepository : IGenericRepository<Servico>
    {
        Task<IEnumerable<Servico>> GetByNameWithDetails(string nome);
        Task<IEnumerable<Servico>> GetByNameWithDetailsForOficina(string nome, int oficinaId);
        Task<Servico?> GetByIdWithDetails(int id);
        Task<Servico?> GetByIdWithDetailsForOficina(int id, int oficinaId);
        Task<IEnumerable<Servico>> GetByOficina(int oficinaId);
        Task<Servico?> GetByIdForOficina(int id, int oficinaId);
        Task<Servico> Add(Servico servico);
    }
}
