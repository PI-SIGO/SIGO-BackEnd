using FluentValidation;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Enums;

namespace SIGO.Validation
{
    public sealed class LoginClienteValidator : AbstractValidator<LoginClienteDTO>
    {
        public LoginClienteValidator(ICpfCnpjValidator cpfCnpjValidator)
        {
            RuleFor(request => request.Documento)
                .NotEmpty().WithMessage("CPF/CNPJ obrigatório.")
                .Must(cpfCnpjValidator.IsValid).WithMessage("CPF/CNPJ inválido.")
                .OverridePropertyName(nameof(LoginClienteDTO.Cpf_Cnpj));

            RuleFor(request => request.Senha)
                .NotEmpty().WithMessage("Senha obrigatória.")
                .MaximumLength(128).WithMessage("Senha deve ter no máximo 128 caracteres.");
        }
    }

    public sealed class CadastrarClienteValidator : AbstractValidator<CadastrarClienteDTO>
    {
        public CadastrarClienteValidator(ICpfCnpjValidator cpfCnpjValidator)
        {
            RuleFor(request => request.Documento)
                .NotEmpty().WithMessage("CPF/CNPJ obrigatório.")
                .Must(cpfCnpjValidator.IsValid).WithMessage("CPF/CNPJ inválido.")
                .OverridePropertyName(nameof(CadastrarClienteDTO.Cpf_Cnpj));

            RuleFor(request => request.Nome)
                .NotEmpty().WithMessage("Nome obrigatório.")
                .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres.");

            RuleFor(request => request.Email)
                .NotEmpty().WithMessage("E-mail obrigatório.")
                .MaximumLength(254).WithMessage("E-mail deve ter no máximo 254 caracteres.")
                .EmailAddress().WithMessage("E-mail inválido.")
                .Must(EmailValidation.IsCanonical)
                .WithMessage("Informe somente o endereço de e-mail, sem nome de exibição.");

            RuleFor(request => request.Senha)
                .NotEmpty().WithMessage("Senha obrigatória.")
                .MinimumLength(8).WithMessage("Senha deve ter pelo menos 8 caracteres.")
                .MaximumLength(128).WithMessage("Senha deve ter no máximo 128 caracteres.")
                .Matches("[A-Za-z]").WithMessage("Senha deve conter ao menos uma letra.")
                .Matches("[0-9]").WithMessage("Senha deve conter ao menos um número.");
        }
    }

    public sealed class PreCadastrarClienteValidator : AbstractValidator<PreCadastrarClienteDTO>
    {
        public PreCadastrarClienteValidator(ICpfCnpjValidator cpfCnpjValidator)
        {
            RuleFor(request => request.Documento)
                .NotEmpty().WithMessage("CPF/CNPJ obrigatório.")
                .Must(cpfCnpjValidator.IsValid).WithMessage("CPF/CNPJ inválido.")
                .OverridePropertyName(nameof(PreCadastrarClienteDTO.Cpf_Cnpj));

            RuleFor(request => request.Nome)
                .NotEmpty().WithMessage("Nome obrigatório.")
                .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres.");

            When(request => !string.IsNullOrWhiteSpace(request.Email), () =>
            {
                RuleFor(request => request.Email!)
                    .MaximumLength(254).WithMessage("E-mail deve ter no máximo 254 caracteres.")
                    .EmailAddress().WithMessage("E-mail inválido.")
                    .Must(EmailValidation.IsCanonical)
                    .WithMessage("Informe somente o endereço de e-mail, sem nome de exibição.");
            });

            When(request => !string.IsNullOrWhiteSpace(request.Telefone), () =>
            {
                RuleFor(request => request.Telefone!)
                    .Must(HasValidLegacyPhone)
                    .WithMessage("Telefone deve conter DDD e 8 ou 9 dígitos.");
            });

            RuleFor(request => request.Obs).MaximumLength(500);
            RuleFor(request => request.Razao).MaximumLength(500);
            RuleFor(request => request.Rua).MaximumLength(500);
            RuleFor(request => request.Cidade).MaximumLength(500);
            RuleFor(request => request.Bairro).MaximumLength(500);
            RuleFor(request => request.Estado).MaximumLength(500);
            RuleFor(request => request.Pais).MaximumLength(500);
            RuleFor(request => request.Complemento).MaximumLength(500);

            When(request => !string.IsNullOrWhiteSpace(request.Cep), () =>
            {
                RuleFor(request => request.Cep!)
                    .Must(value => value.Count(char.IsDigit) == 8)
                    .WithMessage("CEP deve conter 8 dígitos.");
            });

            RuleFor(request => request.DataNasc)
                .Must(value => !value.HasValue || value.Value <= DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("Data de nascimento não pode estar no futuro.");

            RuleFor(request => request.Sexo)
                .Must(value => !value.HasValue || Enum.IsDefined(value.Value))
                .WithMessage("Sexo inválido.");

            RuleFor(request => request.Numero)
                .Must(value => !value.HasValue || value.Value >= 0)
                .WithMessage("Número do endereço não pode ser negativo.");

            RuleFor(request => request.Telefones)
                .Must((request, telefones) =>
                    (telefones?.Count ?? 0) +
                    (string.IsNullOrWhiteSpace(request.Telefone) ? 0 : 1) <= 5)
                .WithMessage("Informe no máximo 5 telefones.")
                .Must(HaveNoDuplicatePhones)
                .WithMessage("Não informe telefones duplicados.");

            RuleForEach(request => request.Telefones)
                .SetValidator(new PreCadastrarTelefoneClienteValidator());
        }

        private static bool HasValidLegacyPhone(string value)
        {
            var digits = new string(value.Where(char.IsDigit).ToArray());
            if (digits.Length is 12 or 13 && digits.StartsWith("55", StringComparison.Ordinal))
                digits = digits[2..];

            return digits.Length is 10 or 11;
        }

        private static bool HaveNoDuplicatePhones(
            IReadOnlyCollection<PreCadastrarTelefoneClienteDTO>? phones)
        {
            if (phones is null)
                return true;

            var normalized = phones
                .Select(phone => $"{phone.DDD}:{new string((phone.Numero ?? string.Empty).Where(char.IsDigit).ToArray())}")
                .ToArray();
            return normalized.Distinct(StringComparer.Ordinal).Count() == normalized.Length;
        }
    }

    public sealed class PreCadastrarTelefoneClienteValidator : AbstractValidator<PreCadastrarTelefoneClienteDTO>
    {
        public PreCadastrarTelefoneClienteValidator()
        {
            RuleFor(request => request.DDD)
                .InclusiveBetween(11, 99)
                .WithMessage("DDD inválido.");
            RuleFor(request => request.Numero)
                .NotEmpty()
                .Must(value => value?.Count(char.IsDigit) is 8 or 9)
                .WithMessage("Número deve conter 8 ou 9 dígitos.");
        }
    }

    public sealed class AlterarSenhaClienteValidator : AbstractValidator<AlterarSenhaClienteDTO>
    {
        public AlterarSenhaClienteValidator()
        {
            RuleFor(request => request.SenhaAtual)
                .NotEmpty().WithMessage("Senha atual obrigatória.");
            RuleFor(request => request.NovaSenha)
                .MinimumLength(8).WithMessage("Nova senha deve ter pelo menos 8 caracteres.")
                .MaximumLength(128).WithMessage("Nova senha deve ter no máximo 128 caracteres.")
                .Matches("[A-Za-z]").WithMessage("Nova senha deve conter ao menos uma letra.")
                .Matches("[0-9]").WithMessage("Nova senha deve conter ao menos um número.")
                .NotEqual(request => request.SenhaAtual)
                .WithMessage("Nova senha deve ser diferente da senha atual.");
        }
    }

    internal static class EmailValidation
    {
        public static bool IsCanonical(string? value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   System.Net.Mail.MailAddress.TryCreate(value.Trim(), out var address) &&
                   string.Equals(address.Address, value.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
