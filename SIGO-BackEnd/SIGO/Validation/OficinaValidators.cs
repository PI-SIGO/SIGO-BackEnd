using FluentValidation;
using SIGO.Objects.Dtos.Entities;

namespace SIGO.Validation
{
    public sealed class OficinaRequestValidator : AbstractValidator<OficinaRequestDTO>
    {
        public OficinaRequestValidator()
        {
            RuleFor(request => request.Cep)
                .GreaterThan(0)
                .WithMessage("CEP deve ser maior que zero.");
        }
    }
}
