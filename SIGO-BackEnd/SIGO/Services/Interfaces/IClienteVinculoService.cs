using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;

namespace SIGO.Services.Interfaces
{
    public interface IClienteVinculoService
    {
        Task<IReadOnlyList<VinculoClienteOficinaResumoDTO>> GetByClientAsync(
            int clienteId,
            CancellationToken cancellationToken = default);

        Task<PreCadastroClienteResultadoDTO> PreRegisterAsync(
            PreCadastrarClienteDTO request,
            int oficinaId,
            SecurityAuditContext auditContext,
            CancellationToken cancellationToken = default);

        Task RevokeAsync(
            int clienteId,
            int oficinaId,
            SecurityAuditContext auditContext,
            CancellationToken cancellationToken = default);

        Task DeactivateForOficinaAsync(
            int clienteId,
            int oficinaId,
            SecurityAuditContext auditContext,
            CancellationToken cancellationToken = default);

        Task<bool> UpdateStatusForOficinaAsync(
            int clienteId,
            int oficinaId,
            bool ativo,
            SecurityAuditContext auditContext,
            CancellationToken cancellationToken = default);
    }
}
