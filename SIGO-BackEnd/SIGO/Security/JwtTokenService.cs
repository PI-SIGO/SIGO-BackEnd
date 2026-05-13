using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SIGO.Security
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly JwtOptions _jwtOptions;

        public JwtTokenService(IOptions<JwtOptions> options)
        {
            _jwtOptions = options?.Value
                ?? throw new InvalidOperationException("Jwt options are not configured.");
        }

        public string GenerateToken(JwtTokenRequest request)
        {
            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtOptions.Key)
            );

            var credentials = new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256
            );

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, request.UserId.ToString()),
                new Claim(ClaimTypes.Name, request.Name),
                new Claim(ClaimTypes.Email, request.Email),
                new Claim(ClaimTypes.Role, request.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (request.OficinaId.HasValue)
                claims.Add(new Claim(CustomClaimTypes.OficinaId, request.OficinaId.Value.ToString()));

            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
