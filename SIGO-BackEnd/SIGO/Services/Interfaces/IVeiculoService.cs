using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;

namespace SIGO.Services.Interfaces
{
    public interface IVeiculoService : IGenericService<Veiculo, VeiculoDTO>
    {
        Task<IEnumerable<VeiculoDTO>> GetByPlaca(string placa);
        Task<IEnumerable<VeiculoDTO>> GetByPlacaForCliente(string placa, int clienteId);
        Task<IEnumerable<VeiculoDTO>> GetByPlacaForOficina(string placa, int oficinaId);
        Task<IEnumerable<VeiculoDTO>> GetByTipo(string tipo);
        Task<IEnumerable<VeiculoDTO>> GetByTipoForCliente(string tipo, int clienteId);
        Task<IEnumerable<VeiculoDTO>> GetByTipoForOficina(string tipo, int oficinaId);
        Task<IEnumerable<VeiculoDTO>> GetByCliente(int clienteId);
        Task<IEnumerable<VeiculoDTO>> GetByOficina(int oficinaId);
        Task<VeiculoDTO?> GetByIdForCliente(int id, int clienteId);
        Task<VeiculoDTO?> GetByIdForOficina(int id, int oficinaId);
        Task CreateForCliente(VeiculoDTO veiculoDto, int clienteId);
        Task CreateForOficina(VeiculoDTO veiculoDto, int oficinaId);
        Task UpdateVeiculoForCliente(VeiculoDTO veiculoDto, int id, int clienteId);
        Task UpdateVeiculoForOficina(VeiculoDTO veiculoDto, int id, int oficinaId);
        Task UpdateVeiculo(VeiculoDTO veiculoDto, int id);
    }
}
