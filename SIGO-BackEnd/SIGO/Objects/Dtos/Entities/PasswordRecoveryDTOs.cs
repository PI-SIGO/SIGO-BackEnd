namespace SIGO.Objects.Dtos.Entities
{
    public sealed record ForgotPasswordRequestDTO
    {
        public string Email { get; init; } = string.Empty;
    }

    public sealed record ValidatePasswordResetTokenDTO
    {
        public string Token { get; init; } = string.Empty;
    }

    public sealed record ResetPasswordDTO
    {
        public string Token { get; init; } = string.Empty;
        public string NewPassword { get; init; } = string.Empty;
        public string ConfirmPassword { get; init; } = string.Empty;
    }

    public sealed record ForgotPasswordResponseDTO(string Message);

    public sealed record PasswordResetTokenValidationResponseDTO(bool Valid);
}
