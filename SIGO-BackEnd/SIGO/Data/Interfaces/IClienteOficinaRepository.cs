namespace SIGO.Data.Interfaces
{
    public interface IClienteOficinaRepository
    {
        Task<bool> ExistsAsync(int oficinaId, int clienteId);
        Task AddIfNotExistsAsync(int oficinaId, int clienteId);
        Task AddOrUpdatePermissoesAsync(int oficinaId, int clienteId, string dadosPermitidos);
    }
}
