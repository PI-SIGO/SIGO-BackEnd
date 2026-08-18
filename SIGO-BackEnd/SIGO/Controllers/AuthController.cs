using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SIGO.Errors;
using SIGO.Objects.Dtos.Entities;
using SIGO.Security;
using SIGO.Services.Interfaces;

namespace SIGO.Controllers
{
    [Route("api/v1/auth")]
    [ApiController]
    public sealed class AuthController : ControllerBase
    {
        public const string GenericRecoveryMessage =
            "Se existir uma conta associada a este e-mail, enviaremos as instruções para redefinição de senha.";

        private readonly IPasswordRecoveryService _passwordRecoveryService;
        private readonly IUnifiedAuthenticationService _unifiedAuthenticationService;

        public AuthController(
            IPasswordRecoveryService passwordRecoveryService,
            IUnifiedAuthenticationService unifiedAuthenticationService)
        {
            _passwordRecoveryService = passwordRecoveryService;
            _unifiedAuthenticationService = unifiedAuthenticationService;
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.PasswordRecovery)]
        [ProducesResponseType(typeof(ForgotPasswordResponseDTO), StatusCodes.Status202Accepted)]
        public async Task<ActionResult<ForgotPasswordResponseDTO>> ForgotPassword(
            [FromBody] ForgotPasswordRequestDTO request,
            CancellationToken cancellationToken)
        {
            await _passwordRecoveryService.RequestPasswordResetAsync(
                request.Email,
                cancellationToken);

            return Accepted(new ForgotPasswordResponseDTO(GenericRecoveryMessage));
        }

        [HttpPost("reset-password/validate")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.PasswordReset)]
        [ProducesResponseType(
            typeof(PasswordResetTokenValidationResponseDTO),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PasswordResetTokenValidationResponseDTO>> ValidateResetToken(
            [FromBody] ValidatePasswordResetTokenDTO request,
            CancellationToken cancellationToken)
        {
            var valid = await _passwordRecoveryService.ValidateTokenAsync(
                request.Token,
                cancellationToken);

            return valid
                ? Ok(new PasswordResetTokenValidationResponseDTO(true))
                : this.ApiProblem(
                    StatusCodes.Status400BadRequest,
                    "Este link de redefinição é inválido ou expirou.");
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.PasswordReset)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword(
            [FromBody] ResetPasswordDTO request,
            CancellationToken cancellationToken)
        {
            var reset = await _passwordRecoveryService.ResetPasswordAsync(
                request,
                cancellationToken);

            return reset
                ? NoContent()
                : this.ApiProblem(
                    StatusCodes.Status400BadRequest,
                    "Este link de redefinição é inválido ou expirou.");
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.UnifiedLogin)]
        public async Task<ActionResult<UnifiedLoginResponseDTO>> Login(
        [FromBody] UnifiedLoginRequestDTO request,
        CancellationToken cancellationToken)
        {
            if (request is null ||
                string.IsNullOrWhiteSpace(request.Identifier) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return this.ApiValidationProblem(
                    StatusCodes.Status400BadRequest,
                    "request",
                    "Informe a credencial e a senha."
                );
            }

            var result = await _unifiedAuthenticationService.AuthenticateAsync(
                request,
                cancellationToken
            );

            if (result is null)
            {
                return this.ApiProblem(
                    StatusCodes.Status401Unauthorized,
                    "Credencial ou senha inválidos."
                );
            }

            return Ok(result);
        }
    }
}
