using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;

namespace SIGO.Services.Interfaces
{
    public interface IClienteAuthenticationService
    {
        Task<ClienteAuthenticationResult?> AuthenticateAsync(
            LoginClienteDTO login,
            CancellationToken cancellationToken = default);

        Task ChangePasswordAsync(
            int clienteId,
            AlterarSenhaClienteDTO request,
            SecurityAuditContext auditContext,
            CancellationToken cancellationToken = default);
    }
}
