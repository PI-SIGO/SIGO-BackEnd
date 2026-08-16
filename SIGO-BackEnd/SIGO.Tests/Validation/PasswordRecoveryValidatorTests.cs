using SIGO.Objects.Dtos.Entities;
using SIGO.Validation;
using Xunit;

namespace SIGO.Tests.Validation;

public sealed class PasswordRecoveryValidatorTests
{
    [Theory]
    [InlineData("")]
    [InlineData("email-invalido")]
    [InlineData("Nome <pessoa@example.com>")]
    public void ForgotPassword_EmailInvalido_DeveRejeitar(string email)
    {
        var result = new ForgotPasswordRequestValidator().Validate(
            new ForgotPasswordRequestDTO { Email = email });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "Email");
    }

    [Fact]
    public void ForgotPassword_EmailValido_DeveAceitar()
    {
        var result = new ForgotPasswordRequestValidator().Validate(
            new ForgotPasswordRequestDTO { Email = "pessoa@example.com" });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "Senha123", "Senha123", "Token")]
    [InlineData("token", "curta", "curta", "NewPassword")]
    [InlineData("token", "NovaSenha123", "OutraSenha123", "ConfirmPassword")]
    public void ResetPassword_DadosInvalidos_DeveRejeitar(
        string token,
        string newPassword,
        string confirmPassword,
        string property)
    {
        var result = new ResetPasswordValidator().Validate(new ResetPasswordDTO
        {
            Token = token,
            NewPassword = newPassword,
            ConfirmPassword = confirmPassword
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == property);
    }

    [Fact]
    public void ResetPassword_DadosValidos_DeveAceitar()
    {
        var result = new ResetPasswordValidator().Validate(new ResetPasswordDTO
        {
            Token = "token",
            NewPassword = "NovaSenha123",
            ConfirmPassword = "NovaSenha123"
        });

        Assert.True(result.IsValid);
    }
}
