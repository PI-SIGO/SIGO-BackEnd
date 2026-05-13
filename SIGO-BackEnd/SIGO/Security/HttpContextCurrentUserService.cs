using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace SIGO.Security
{
    public class HttpContextCurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpContextCurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? UserId
        {
            get
            {
                var idClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(idClaim, out var id) ? id : null;
            }
        }

        public int? OficinaId
        {
            get
            {
                var oficinaIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(CustomClaimTypes.OficinaId)?.Value;
                return int.TryParse(oficinaIdClaim, out var oficinaId) ? oficinaId : null;
            }
        }

        public bool IsInRole(string role)
        {
            return _httpContextAccessor.HttpContext?.User.IsInRole(role) == true;
        }
    }
}
