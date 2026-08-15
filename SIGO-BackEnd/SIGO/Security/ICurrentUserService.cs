namespace SIGO.Security
{
    public interface ICurrentUserService
    {
        int? UserId { get; }
        int? OficinaId { get; }
        string? UserName { get; }
        bool IsInRole(string role);
    }
}
