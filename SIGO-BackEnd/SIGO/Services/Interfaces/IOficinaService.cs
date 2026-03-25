using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;

namespace SIGO.Services.Interfaces
{
    public interface IOficinaService : IGenericService<Oficina, OficinaDTO>
    {
        Task<IEnumerable<OficinaDTO>> GetByName(string nomeOficina);
        Task ValidarCnpj(string? cnpj, int? ignoreId = null);
    }
}
