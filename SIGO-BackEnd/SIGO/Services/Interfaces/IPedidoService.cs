using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;

namespace SIGO.Services.Interfaces
{
    public interface IPedidoService : IGenericService<Pedido, PedidoDTO>
    {
        Task<IEnumerable<PedidoDTO>> GetByOficina(int oficinaId);
        Task<IEnumerable<PedidoDTO>> GetByCliente(int clienteId);
        Task<PedidoDTO?> GetByIdForOficina(int id, int oficinaId);
        Task CreateForOficina(PedidoDTO pedidoDTO, int oficinaId);
        Task UpdateForOficina(PedidoDTO pedidoDTO, int id, int oficinaId);
    }
}
