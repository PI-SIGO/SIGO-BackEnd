using FluentValidation;
using SIGO.Objects.Dtos.Entities;

namespace SIGO.Validation
{
    public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequestDTO>
    {
        public ForgotPasswordRequestValidator()
        {
            RuleFor(request => request.Email)
                .NotEmpty().WithMessage("E-mail obrigatório.")
                .MaximumLength(254).WithMessage("E-mail deve ter no máximo 254 caracteres.")
                .EmailAddress().WithMessage("E-mail inválido.")
                .Must(EmailValidation.IsCanonical)
                .WithMessage("Informe somente o endereço de e-mail, sem nome de exibição.");
        }
    }

    public sealed class ValidatePasswordResetTokenValidator
        : AbstractValidator<ValidatePasswordResetTokenDTO>
    {
        public ValidatePasswordResetTokenValidator()
        {
            RuleFor(request => request.Token)
                .NotEmpty().WithMessage("Token obrigatório.")
                .MaximumLength(512).WithMessage("Token inválido.");
        }
    }

    public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordDTO>
    {
        public ResetPasswordValidator()
        {
            RuleFor(request => request.Token)
                .NotEmpty().WithMessage("Token obrigatório.")
                .MaximumLength(512).WithMessage("Token inválido.");

            RuleFor(request => request.NewPassword)
                .NotEmpty().WithMessage("Nova senha obrigatória.")
                .MinimumLength(8).WithMessage("Nova senha deve ter pelo menos 8 caracteres.")
                .MaximumLength(128).WithMessage("Nova senha deve ter no máximo 128 caracteres.")
                .Matches("[A-Za-z]").WithMessage("Nova senha deve conter ao menos uma letra.")
                .Matches("[0-9]").WithMessage("Nova senha deve conter ao menos um número.");

            RuleFor(request => request.ConfirmPassword)
                .NotEmpty().WithMessage("Confirmação da nova senha obrigatória.")
                .Equal(request => request.NewPassword)
                .WithMessage("A confirmação da nova senha não confere.");
        }
    }
}
