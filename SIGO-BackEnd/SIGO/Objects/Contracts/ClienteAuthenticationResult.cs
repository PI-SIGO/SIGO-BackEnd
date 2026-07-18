namespace SIGO.Objects.Contracts
{
    public sealed record ClienteAuthenticationResult(
        int ClienteId,
        string Nome,
        string Email,
        int TokenVersion);
}
