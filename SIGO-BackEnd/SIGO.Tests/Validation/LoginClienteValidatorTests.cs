using SIGO.Objects.Dtos.Entities;
using SIGO.Validation;
using Xunit;

namespace SIGO.Tests.Validation;

public sealed class LoginClienteValidatorTests
{
    private readonly LoginClienteValidator _validator = new(new CpfValidator());

    [Fact]
    public void Validate_CpfFormatadoESenhaPreenchida_DeveSerValido()
    {
        var result = _validator.Validate(new LoginClienteDTO
        {
            Cpf = "529.982.247-25",
            Senha = "Senha123"
        });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "Senha123", nameof(LoginClienteDTO.Cpf))]
    [InlineData("11111111111", "Senha123", nameof(LoginClienteDTO.Cpf))]
    [InlineData("52998224725", "", nameof(LoginClienteDTO.Senha))]
    public void Validate_CredencialInvalida_DeveRejeitar(
        string cpf,
        string senha,
        string propriedade)
    {
        var result = _validator.Validate(new LoginClienteDTO
        {
            Cpf = cpf,
            Senha = senha
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == propriedade);
    }
}
