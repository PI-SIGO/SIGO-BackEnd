using SIGO.Objects.Models;

namespace SIGO.Data.Interfaces
{
    public interface IRegistroServicoRepository
    {
        Task<IEnumerable<RegistroServico>> GetByVeiculoAsync(int veiculoId, DateTime? from = null, DateTime? to = null, string? tipoServico = null);
    }
}
