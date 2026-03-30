using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;

namespace SIGO.Services.Interfaces
{
    public interface IClienteService : IGenericService<Cliente, ClienteDTO>
    {
        Task<IEnumerable<ClienteDTO>> GetByNameWithDetails(string nome);
        Task<ClienteDTO?> GetByIdWithDetails(int id);
        Task<ClienteDTO> Login(Login login);
        Task ValidarCpfCnpj(string? documento, int? ignoreId = null);
        Task ValidarNomeEmail(string? nome, string? email, int? ignoreId = null);
    }
}
