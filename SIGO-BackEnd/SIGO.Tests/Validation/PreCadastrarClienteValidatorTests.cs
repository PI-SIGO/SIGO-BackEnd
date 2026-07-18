using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Enums;
using SIGO.Validation;
using Xunit;

namespace SIGO.Tests.Validation;

public sealed class PreCadastrarClienteValidatorTests
{
    private readonly PreCadastrarClienteValidator _validator = new(new CpfValidator());

    [Fact]
    public void Validate_CadastroCompletoValido_DeveSerValido()
    {
        var result = _validator.Validate(CreateValidRequest());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EnderecoDataETelefoneInvalidos_DeveRejeitar()
    {
        var request = CreateValidRequest() with
        {
            Cep = "123",
            DataNasc = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            Numero = -1,
            Telefones = new[]
            {
                new PreCadastrarTelefoneClienteDTO { DDD = 1, Numero = "123" }
            }
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(PreCadastrarClienteDTO.Cep));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(PreCadastrarClienteDTO.DataNasc));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(PreCadastrarClienteDTO.Numero));
        Assert.Contains(result.Errors, error => error.PropertyName.Contains(nameof(PreCadastrarClienteDTO.Telefones)));
    }

    [Fact]
    public void Validate_TelefonesDuplicados_DeveRejeitar()
    {
        var request = CreateValidRequest() with
        {
            Telefones = new[]
            {
                new PreCadastrarTelefoneClienteDTO { DDD = 47, Numero = "99999-9999" },
                new PreCadastrarTelefoneClienteDTO { DDD = 47, Numero = "999999999" }
            }
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(PreCadastrarClienteDTO.Telefones) &&
            error.ErrorMessage.Contains("duplicados", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_SexoForaDoEnum_DeveRejeitar()
    {
        var request = CreateValidRequest() with { Sexo = (Sexo)999 };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(PreCadastrarClienteDTO.Sexo));
    }

    private static PreCadastrarClienteDTO CreateValidRequest() => new()
    {
        Cpf = "52998224725",
        Nome = "Cliente Completo",
        Email = "cliente@example.com",
        DataNasc = new DateOnly(1950, 5, 10),
        Sexo = Sexo.Feminino,
        Numero = 120,
        Rua = "Rua das Flores",
        Cidade = "Blumenau",
        Cep = "89010-000",
        Bairro = "Centro",
        Estado = "SC",
        Pais = "Brasil",
        Complemento = "Casa",
        Telefones = new[]
        {
            new PreCadastrarTelefoneClienteDTO { DDD = 47, Numero = "99999-9999" }
        }
    };
}
