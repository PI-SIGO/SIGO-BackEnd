using SIGO.Objects.Contracts;
using SIGO.Objects.Models;

namespace SIGO.Data.Interfaces
{
    public interface IPasswordRecoveryRepository
    {
        Task<IReadOnlyList<PasswordRecoveryAccount>> FindActiveAccountsByEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default);

        Task CreateTokenAsync(
            TokenRedefinicaoSenha token,
            DateTime invalidatedAt,
            CancellationToken cancellationToken = default);

        Task<bool> IsTokenValidAsync(
            string tokenHash,
            DateTime utcNow,
            CancellationToken cancellationToken = default);

        Task<bool> ResetPasswordAsync(
            string tokenHash,
            string newPasswordHash,
            DateTime utcNow,
            CancellationToken cancellationToken = default);
    }
}
