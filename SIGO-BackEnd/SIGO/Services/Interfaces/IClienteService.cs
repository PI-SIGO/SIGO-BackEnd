using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;

namespace SIGO.Services.Interfaces
{
    public interface IClienteService : IGenericService<Cliente, ClienteDTO>
    {
        Task<IEnumerable<ClienteDTO>> GetByNameWithDetails(string nome);
        Task<IEnumerable<ClienteOficinaDTO>> GetByOficina(int oficinaId);
        Task<IEnumerable<ClienteOficinaDTO>> GetByNameWithDetailsForOficina(string nome, int oficinaId);
        Task<ClienteDTO?> GetByIdWithDetails(int id);
        Task<ClienteOficinaDTO?> GetByIdWithDetailsForOficina(int id, int oficinaId);
        Task<bool> ExistsInOficina(int clienteId, int oficinaId);
        Task ValidarCpfCnpj(string? documento, int? ignoreId = null);
        Task ValidarNomeEmail(string? nome, string? email, int? ignoreId = null);
        Task Update(ClienteRequestDTO clienteDTO, int id);
        Task<ClienteOficinaDTO> UpdateForOficina(
            ClienteRequestDTO clienteDTO,
            int clienteId,
            int oficinaId);
        Task<bool> DeactivateAsync(int id, CancellationToken cancellationToken = default);
    }
}
