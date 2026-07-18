using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;

namespace SIGO.Services.Interfaces
{
    public interface IClienteRegistrationService
    {
        Task<CadastroClienteResultadoDTO> RegisterAsync(
            CadastrarClienteDTO request,
            SecurityAuditContext auditContext,
            CancellationToken cancellationToken = default);
    }
}
