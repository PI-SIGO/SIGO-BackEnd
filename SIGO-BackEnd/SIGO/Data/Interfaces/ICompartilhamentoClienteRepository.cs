using SIGO.Objects.Models;

namespace SIGO.Data.Interfaces
{
    public interface ICompartilhamentoClienteRepository
    {
        Task<ICompartilhamentoClienteTransaction> BeginTransactionAsync();
        Task AddAsync(CompartilhamentoCliente compartilhamento);
        Task<bool> ExistsByCodeHashAsync(string codigoHash);
        Task<CompartilhamentoCliente?> GetByCodeHashAsync(string codigoHash);
        Task<CompartilhamentoCliente?> GetValidByCodeHashAsync(string codigoHash, DateTime agoraUtc);
        Task<CompartilhamentoCliente?> RedeemValidByCodeHashAsync(string codigoHash, DateTime agoraUtc);
        Task AddTentativaAsync(CompartilhamentoClienteTentativa tentativa);
        Task<int> CountFalhasRecentesAsync(int oficinaId, string? ipAddress, DateTime desdeUtc);
        Task SaveChangesAsync();
    }

    public interface ICompartilhamentoClienteTransaction : IAsyncDisposable
    {
        Task CommitAsync();
    }
}
