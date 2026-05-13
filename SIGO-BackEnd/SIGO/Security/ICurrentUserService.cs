namespace SIGO.Security
{
    public interface ICurrentUserService
    {
        int? UserId { get; }
        int? OficinaId { get; }
        bool IsInRole(string role);
    }
}
