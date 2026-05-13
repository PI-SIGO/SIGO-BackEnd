using SIGO.Objects.Models;

namespace SIGO.Data.Interfaces
{
    public interface IVeiculoRepository : IGenericRepository<Veiculo>
    {
        Task<IEnumerable<Veiculo>> GetByPlaca(string placa);
        Task<IEnumerable<Veiculo>> GetByPlacaForCliente(string placa, int clienteId);
        Task<IEnumerable<Veiculo>> GetByPlacaForOficina(string placa, int oficinaId);
        Task<IEnumerable<Veiculo>> GetByTipo(string tipo);
        Task<IEnumerable<Veiculo>> GetByTipoForCliente(string tipo, int clienteId);
        Task<IEnumerable<Veiculo>> GetByTipoForOficina(string tipo, int oficinaId);
        Task<IEnumerable<Veiculo>> GetByCliente(int clienteId);
        Task<IEnumerable<Veiculo>> GetByOficina(int oficinaId);
        Task<Veiculo?> GetByIdForCliente(int id, int clienteId);
        Task<Veiculo?> GetByIdForOficina(int id, int oficinaId);
        Task UpdateVeiculo(Veiculo veiculo);
    }
}
