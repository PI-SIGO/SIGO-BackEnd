using SIGO.Objects.Dtos.Entities;

namespace SIGO.Services.Interfaces
{
    public interface IPasswordRecoveryService
    {
        Task RequestPasswordResetAsync(
            string email,
            CancellationToken cancellationToken = default);

        Task<bool> ValidateTokenAsync(
            string token,
            CancellationToken cancellationToken = default);

        Task<bool> ResetPasswordAsync(
            ResetPasswordDTO request,
            CancellationToken cancellationToken = default);
    }
}
