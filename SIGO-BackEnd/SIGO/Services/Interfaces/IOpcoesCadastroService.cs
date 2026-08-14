using SIGO.Objects.Dtos.Entities;

namespace SIGO.Services.Interfaces
{
    public interface IOpcoesCadastroService
    {
        Task<OpcoesCadastroDTO> GetByOficinaAsync(
            int oficinaId,
            CancellationToken cancellationToken = default);
    }
}
