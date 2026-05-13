using SIGO.Security;

namespace SIGO.Tests.TestSupport
{
    internal sealed class TestCurrentUserService : ICurrentUserService
    {
        private readonly HashSet<string> _roles;

        public TestCurrentUserService(int? userId = null, int? oficinaId = null, params string[] roles)
        {
            UserId = userId;
            OficinaId = oficinaId;
            _roles = roles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public int? UserId { get; }
        public int? OficinaId { get; }

        public bool IsInRole(string role) => _roles.Contains(role);
    }
}
