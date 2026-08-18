namespace SIGO.Objects.Dtos.Entities
{
    public sealed record UnifiedLoginRequestDTO
    {
        public string Identifier { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
    }

    public sealed record UnifiedLoginResponseDTO(
        string AccessToken,
        string Role,
        string TokenType = "Bearer"
    );
}