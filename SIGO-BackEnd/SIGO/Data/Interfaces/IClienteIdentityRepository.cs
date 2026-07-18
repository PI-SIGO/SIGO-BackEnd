using SIGO.Objects.Enums;
using SIGO.Objects.Models;

namespace SIGO.Data.Interfaces
{
    public interface IClienteIdentityRepository
    {
        Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default);

        Task<Cliente?> GetClienteByCpfAsync(
            string cpfNormalizado,
            CancellationToken cancellationToken = default);

        Task<ClienteContato?> GetContatoAsync(
            int clienteId,
            TipoContatoCliente tipo,
            string valorNormalizado,
            CancellationToken cancellationToken = default);

        Task AddClienteAsync(Cliente cliente, CancellationToken cancellationToken = default);
        Task AddContatoAsync(ClienteContato contato, CancellationToken cancellationToken = default);
        Task AddAuditoriaAsync(AuditoriaSeguranca auditoria, CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
