using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SIGO.Data.Interfaces;
using SIGO.Integracao.Interfaces;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Enums;
using SIGO.Objects.Models;
using SIGO.Security;
using SIGO.Services.Entities;
using Xunit;

namespace SIGO.Tests.Services;

public sealed class PasswordRecoveryServiceTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 8, 15, 18, 0, 0, TimeSpan.Zero);

    private readonly Mock<IPasswordRecoveryRepository> _repository = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();

    [Fact]
    public async Task RequestPasswordResetAsync_DevePersistirSomenteHashEEnviarLink()
    {
        TokenRedefinicaoSenha? savedToken = null;
        EmailMessage? sentMessage = null;
        _repository
            .Setup(repository => repository.FindActiveAccountsByEmailAsync(
                "cliente@example.com",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new PasswordRecoveryAccount(
                    TipoContaRecuperacao.Cliente,
                    42,
                    "Cliente Teste",
                    "cliente@example.com")
            });
        _repository
            .Setup(repository => repository.CreateTokenAsync(
                It.IsAny<TokenRedefinicaoSenha>(),
                FixedNow.UtcDateTime,
                It.IsAny<CancellationToken>()))
            .Callback<TokenRedefinicaoSenha, DateTime, CancellationToken>(
                (token, _, _) => savedToken = token)
            .Returns(Task.CompletedTask);
        _emailService
            .Setup(service => service.SendAsync(
                It.IsAny<EmailMessage>(),
                It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>(
                (message, _) => sentMessage = message)
            .Returns(Task.CompletedTask);

        await CreateService().RequestPasswordResetAsync("  CLIENTE@EXAMPLE.COM  ");

        Assert.NotNull(savedToken);
        Assert.NotNull(sentMessage);
        Assert.Matches("^[A-F0-9]{64}$", savedToken!.TokenHash);
        Assert.Equal(FixedNow.UtcDateTime, savedToken.CriadoEm);
        Assert.Equal(FixedNow.AddMinutes(30).UtcDateTime, savedToken.ExpiraEm);
        Assert.Contains("conta Cliente", sentMessage!.HtmlBody);

        var match = Regex.Match(sentMessage.HtmlBody, @"token=([^""&]+)");
        Assert.True(match.Success);
        var rawToken = Uri.UnescapeDataString(match.Groups[1].Value);
        Assert.Equal(ComputeHash(rawToken), savedToken.TokenHash);
        Assert.DoesNotContain(savedToken.TokenHash, sentMessage.HtmlBody);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_EmailSemConta_NaoDeveCriarTokenNemEnviarEmail()
    {
        _repository
            .Setup(repository => repository.FindActiveAccountsByEmailAsync(
                "unknown@example.com",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PasswordRecoveryAccount>());

        await CreateService().RequestPasswordResetAsync("unknown@example.com");

        _repository.Verify(
            repository => repository.CreateTokenAsync(
                It.IsAny<TokenRedefinicaoSenha>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _emailService.Verify(
            service => service.SendAsync(
                It.IsAny<EmailMessage>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_EmailEmTiposDiferentes_DeveEmitirUmTokenPorConta()
    {
        _repository
            .Setup(repository => repository.FindActiveAccountsByEmailAsync(
                "shared@example.com",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new PasswordRecoveryAccount(
                    TipoContaRecuperacao.Funcionario,
                    7,
                    "Funcionário",
                    "shared@example.com"),
                new PasswordRecoveryAccount(
                    TipoContaRecuperacao.Oficina,
                    9,
                    "Oficina",
                    "shared@example.com")
            });
        _repository
            .Setup(repository => repository.CreateTokenAsync(
                It.IsAny<TokenRedefinicaoSenha>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _emailService
            .Setup(service => service.SendAsync(
                It.IsAny<EmailMessage>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await CreateService().RequestPasswordResetAsync("shared@example.com");

        _repository.Verify(
            repository => repository.CreateTokenAsync(
                It.IsAny<TokenRedefinicaoSenha>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        _emailService.Verify(
            service => service.SendAsync(
                It.IsAny<EmailMessage>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ResetPasswordAsync_TokenValido_DeveGerarBcryptEConsumirToken()
    {
        const string rawToken = "token-seguro";
        var tokenHash = ComputeHash(rawToken);
        _repository
            .Setup(repository => repository.IsTokenValidAsync(
                tokenHash,
                FixedNow.UtcDateTime,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _passwordHasher
            .Setup(hasher => hasher.Hash("NovaSenha123"))
            .Returns("bcrypt-hash");
        _repository
            .Setup(repository => repository.ResetPasswordAsync(
                tokenHash,
                "bcrypt-hash",
                FixedNow.UtcDateTime,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var reset = await CreateService().ResetPasswordAsync(new ResetPasswordDTO
        {
            Token = rawToken,
            NewPassword = "NovaSenha123",
            ConfirmPassword = "NovaSenha123"
        });

        Assert.True(reset);
        _passwordHasher.Verify(hasher => hasher.Hash("NovaSenha123"), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_TokenInvalido_NaoDeveExecutarHash()
    {
        _repository
            .Setup(repository => repository.IsTokenValidAsync(
                It.IsAny<string>(),
                FixedNow.UtcDateTime,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var reset = await CreateService().ResetPasswordAsync(new ResetPasswordDTO
        {
            Token = "invalido",
            NewPassword = "NovaSenha123",
            ConfirmPassword = "NovaSenha123"
        });

        Assert.False(reset);
        _passwordHasher.Verify(
            hasher => hasher.Hash(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_FalhaInterna_NaoDeveRevelarExcecao()
    {
        _repository
            .Setup(repository => repository.FindActiveAccountsByEmailAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database unavailable"));

        var exception = await Record.ExceptionAsync(
            () => CreateService().RequestPasswordResetAsync("cliente@example.com"));

        Assert.Null(exception);
    }

    private PasswordRecoveryService CreateService()
    {
        return new PasswordRecoveryService(
            _repository.Object,
            _emailService.Object,
            _passwordHasher.Object,
            Options.Create(new PasswordRecoveryOptions
            {
                FrontendBaseUrl = "https://sigo.example.com",
                TokenLifetimeMinutes = 30
            }),
            new FixedTimeProvider(FixedNow),
            NullLogger<PasswordRecoveryService>.Instance);
    }

    private static string ComputeHash(string token)
    {
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
