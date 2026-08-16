using SIGO.Objects.Enums;

namespace SIGO.Objects.Contracts
{
    public sealed record PasswordRecoveryAccount(
        TipoContaRecuperacao AccountType,
        int AccountId,
        string DisplayName,
        string Email);
}
