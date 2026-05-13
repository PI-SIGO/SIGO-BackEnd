using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;

namespace SIGO.Data.Interfaces
{
    public interface ITelefoneRepository : IGenericRepository<Telefone>
    {
        Task<IEnumerable<TelefoneDTO>> GetTelefoneByNome(string nome);
        Task<IEnumerable<TelefoneDTO>> GetTelefoneByNomeForOficina(string nome, int oficinaId);
        Task<IReadOnlyCollection<int>> GetInvalidIdsForCliente(int clienteId, IEnumerable<int> telefoneIds);
        Task<bool> UpdateForCliente(Telefone telefone, int clienteId);
    }
}
