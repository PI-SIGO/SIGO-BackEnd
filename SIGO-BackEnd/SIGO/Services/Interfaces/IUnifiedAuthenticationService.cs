using SIGO.Objects.Dtos.Entities;

namespace SIGO.Services.Interfaces
{
    public interface IUnifiedAuthenticationService
    {
        Task<UnifiedLoginResponseDTO?> AuthenticateAsync(
            UnifiedLoginRequestDTO request,
            CancellationToken cancellationToken = default
        );
    }
}