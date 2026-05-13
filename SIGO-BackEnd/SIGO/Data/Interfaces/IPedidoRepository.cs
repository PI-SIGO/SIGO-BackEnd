using SIGO.Objects.Models;

namespace SIGO.Data.Interfaces
{
    public interface IPedidoRepository : IGenericRepository<Pedido>
    {
        Task<IEnumerable<Pedido>> GetByOficina(int oficinaId);
        Task<IEnumerable<Pedido>> GetByCliente(int clienteId);
        Task<Pedido?> GetByIdForOficina(int id, int oficinaId);
        Task<IEnumerable<Pedido>> GetByVeiculoWithDetailsAsync(int veiculoId);
    }
}
