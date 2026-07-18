using SIGO.Objects.Enums;

namespace SIGO.Objects.Contracts
{
    public sealed record SecurityAuditContext(
        TipoAtorAuditoria TipoAtor,
        int? AtorId,
        string? IpAddress,
        string? CorrelationId);
}
