using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;

namespace SIGO.Services.Interfaces
{
    public interface ITelefoneService : IGenericService<Telefone, TelefoneDTO>
    {
        Task<IEnumerable<TelefoneDTO>> GetTelefoneByNome(string nome);
        Task<IEnumerable<TelefoneDTO>> GetTelefoneByNomeForOficina(string nome, int oficinaId);
    }
}
