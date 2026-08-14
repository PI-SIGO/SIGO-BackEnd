using SIGO.Objects.Models;

namespace SIGO.Data.Interfaces
{
    public interface IOpcaoCadastroRepository
    {
        Task<IReadOnlyList<OpcaoCadastro>> GetByOficinaAsync(
            int oficinaId,
            CancellationToken cancellationToken = default);
    }
}
