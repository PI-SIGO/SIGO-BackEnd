using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using SIGO.Data.Interfaces;
using SIGO.Integracao.Interfaces;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Enums;
using SIGO.Objects.Models;
using SIGO.Security;
using SIGO.Services.Interfaces;
using SIGO.Validation;

namespace SIGO.Services.Entities
{
    public sealed class PasswordRecoveryService : IPasswordRecoveryService
    {
        private readonly IPasswordRecoveryRepository _repository;
        private readonly IEmailService _emailService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly PasswordRecoveryOptions _options;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<PasswordRecoveryService> _logger;

        public PasswordRecoveryService(
            IPasswordRecoveryRepository repository,
            IEmailService emailService,
            IPasswordHasher passwordHasher,
            IOptions<PasswordRecoveryOptions> options,
            TimeProvider timeProvider,
            ILogger<PasswordRecoveryService> logger)
        {
            _repository = repository;
            _emailService = emailService;
            _passwordHasher = passwordHasher;
            _options = options?.Value
                ?? throw new InvalidOperationException(
                    "Password recovery options are not configured.");
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task RequestPasswordResetAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var normalizedEmail = EmailNormalizer.Normalize(email);
                var accounts = await _repository.FindActiveAccountsByEmailAsync(
                    normalizedEmail,
                    cancellationToken);

                foreach (var account in accounts)
                {
                    await IssueTokenAndSendEmailAsync(account, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Password recovery request could not be completed.");
            }
        }

        public Task<bool> ValidateTokenAsync(
            string token,
            CancellationToken cancellationToken = default)
        {
            return _repository.IsTokenValidAsync(
                ComputeTokenHash(token),
                UtcNow(),
                cancellationToken);
        }

        public async Task<bool> ResetPasswordAsync(
            ResetPasswordDTO request,
            CancellationToken cancellationToken = default)
        {
            var tokenHash = ComputeTokenHash(request.Token);
            if (!await _repository.IsTokenValidAsync(
                    tokenHash,
                    UtcNow(),
                    cancellationToken))
            {
                return false;
            }

            var newPasswordHash = _passwordHasher.Hash(request.NewPassword);
            return await _repository.ResetPasswordAsync(
                tokenHash,
                newPasswordHash,
                UtcNow(),
                cancellationToken);
        }

        private async Task IssueTokenAndSendEmailAsync(
            PasswordRecoveryAccount account,
            CancellationToken cancellationToken)
        {
            var utcNow = UtcNow();
            var rawToken = WebEncoders.Base64UrlEncode(
                RandomNumberGenerator.GetBytes(32));
            var token = new TokenRedefinicaoSenha
            {
                TipoConta = account.AccountType,
                ContaId = account.AccountId,
                TokenHash = ComputeTokenHash(rawToken),
                CriadoEm = utcNow,
                ExpiraEm = utcNow.AddMinutes(_options.TokenLifetimeMinutes)
            };

            await _repository.CreateTokenAsync(token, utcNow, cancellationToken);

            try
            {
                var accountLabel = GetAccountLabel(account.AccountType);
                await _emailService.SendAsync(
                    new EmailMessage(
                        account.Email,
                        $"Redefinição de senha — SIGO ({accountLabel})",
                        BuildEmailBody(account, accountLabel, rawToken)),
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Password recovery email delivery failed for account type {AccountType} and id {AccountId}.",
                    account.AccountType,
                    account.AccountId);
            }
        }

        private string BuildEmailBody(
            PasswordRecoveryAccount account,
            string accountLabel,
            string rawToken)
        {
            var frontendRoot = new Uri(
                _options.FrontendBaseUrl.TrimEnd('/') + "/",
                UriKind.Absolute);
            var resetUri = new UriBuilder(new Uri(frontendRoot, "redefinir-senha"))
            {
                Query = $"token={Uri.EscapeDataString(rawToken)}"
            }.Uri.AbsoluteUri;

            var safeName = WebUtility.HtmlEncode(account.DisplayName);
            var safeAccountLabel = WebUtility.HtmlEncode(accountLabel);
            var safeResetUri = WebUtility.HtmlEncode(resetUri);

            return $"""
                <!doctype html>
                <html lang="pt-BR">
                  <body style="margin:0;background:#f7faff;color:#172033;font-family:Arial,sans-serif">
                    <div style="max-width:560px;margin:32px auto;padding:32px;border:1px solid #d8e3f2;border-radius:12px;background:#ffffff">
                      <p style="margin:0 0 8px;color:#075fbd;font-size:14px;font-weight:700">Segurança da conta</p>
                      <h1 style="margin:0 0 20px;font-size:24px">Redefinição de senha — SIGO</h1>
                      <p style="line-height:1.6">Olá, {safeName}.</p>
                      <p style="line-height:1.6">Recebemos uma solicitação para redefinir a senha da conta {safeAccountLabel}.</p>
                      <p style="margin:28px 0">
                        <a href="{safeResetUri}" style="display:inline-block;padding:14px 20px;border-radius:8px;background:#075fbd;color:#ffffff;font-weight:700;text-decoration:none">
                          Redefinir minha senha
                        </a>
                      </p>
                      <p style="line-height:1.6">Este link expira em {_options.TokenLifetimeMinutes} minutos e pode ser usado uma única vez.</p>
                      <p style="margin-bottom:0;line-height:1.6;color:#64748b">Se você não solicitou esta alteração, ignore este e-mail.</p>
                    </div>
                  </body>
                </html>
                """;
        }

        private static string GetAccountLabel(TipoContaRecuperacao accountType)
        {
            return accountType switch
            {
                TipoContaRecuperacao.Cliente => "Cliente",
                TipoContaRecuperacao.Funcionario => "Funcionário",
                TipoContaRecuperacao.Oficina => "Oficina",
                _ => "SIGO"
            };
        }

        private static string ComputeTokenHash(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }

        private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;
    }
}
