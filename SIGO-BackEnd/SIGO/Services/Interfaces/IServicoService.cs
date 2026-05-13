using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;

namespace SIGO.Services.Interfaces
{
    public interface IServicoService : IGenericService<Servico, ServicoDTO>
    {
        Task<IEnumerable<ServicoDTO>> GetByNameWithDetails(string nome);
        Task<IEnumerable<ServicoDTO>> GetByNameWithDetailsForOficina(string nome, int oficinaId);
        Task<ServicoDTO?> GetByIdWithDetails(int id);
        Task<ServicoDTO?> GetByIdWithDetailsForOficina(int id, int oficinaId);
        Task<IEnumerable<ServicoDTO>> GetByOficina(int oficinaId);
        Task<ServicoDTO?> GetByIdForOficina(int id, int oficinaId);
        Task CreateForOficina(ServicoDTO servicoDTO, int oficinaId);
        Task UpdateForOficina(ServicoDTO servicoDTO, int id, int oficinaId);
    }
}
