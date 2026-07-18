namespace SIGO.Objects.Contracts;

/// <summary>
/// Represents a successful bearer-token authentication response.
/// </summary>
public sealed record AccessTokenResponse(
    string AccessToken,
    string TokenType = "Bearer");
