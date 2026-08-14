using FluentValidation;
using SIGO.Objects.Dtos.Entities;

namespace SIGO.Validation;

public sealed class VeiculoValidator : AbstractValidator<VeiculoRequestDTO>
{
    public VeiculoValidator()
    {
        RuleFor(request => request.NomeVeiculo).NotEmpty().MaximumLength(100);
        RuleFor(request => request.ModeloVeiculo).NotEmpty().MaximumLength(50);
        RuleFor(request => request.PlacaVeiculo).NotEmpty().MaximumLength(8);
        RuleFor(request => request.ChassiVeiculo).MaximumLength(17);
        RuleFor(request => request.AnoFab).InclusiveBetween(1886, DateTime.UtcNow.Year + 1);
        RuleFor(request => request.Quilometragem).GreaterThanOrEqualTo(0);
        RuleFor(request => request.Combustivel).NotEmpty().MaximumLength(30);
        RuleFor(request => request.Seguro).MaximumLength(100);
        RuleFor(request => request.Cor).NotEmpty().MaximumLength(50);
    }
}

public sealed class MarcaValidator : AbstractValidator<MarcaDTO>
{
    public MarcaValidator()
    {
        RuleFor(request => request.Nome).NotEmpty().MaximumLength(100);
        RuleFor(request => request.Desc).MaximumLength(500);
        RuleFor(request => request.TipoMarca).NotEmpty().MaximumLength(50);
    }
}

public sealed class TelefoneValidator : AbstractValidator<TelefoneDTO>
{
    public TelefoneValidator()
    {
        RuleFor(request => request.DDD).InclusiveBetween(11, 99);
        RuleFor(request => request.Numero)
            .NotEmpty()
            .Must(numero =>
            {
                var digitCount = numero?.Count(char.IsDigit) ?? 0;
                return digitCount is 8 or 9;
            })
            .WithMessage("Número deve conter 8 ou 9 dígitos.");
        RuleFor(request => request.ClienteId).GreaterThan(0);
    }
}
