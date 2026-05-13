using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;

namespace SIGO.Services.Interfaces
{
    public interface IPecaService : IGenericService<Peca, PecaDTO>
    {
        Task<IEnumerable<PecaDTO>> GetByOficina(int oficinaId);
        Task<PecaDTO?> GetByIdForOficina(int id, int oficinaId);
        Task CreateForOficina(PecaDTO pecaDTO, int oficinaId);
        Task UpdateForOficina(PecaDTO pecaDTO, int id, int oficinaId);
    }
}
