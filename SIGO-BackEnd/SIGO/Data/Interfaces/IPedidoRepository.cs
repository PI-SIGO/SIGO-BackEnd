using SIGO.Objects.Models;

namespace SIGO.Data.Interfaces
{
    public interface IPedidoRepository : IGenericRepository<Pedido>
    {
        Task<IEnumerable<Pedido>> GetByVeiculoWithDetailsAsync(int veiculoId);
    }
}
