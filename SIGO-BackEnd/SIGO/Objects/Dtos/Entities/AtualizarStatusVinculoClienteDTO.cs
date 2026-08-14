namespace SIGO.Objects.Dtos.Entities
{
    public sealed record AtualizarStatusVinculoClienteRequestDTO
    {
        public bool? Ativo { get; init; }
    }

    public sealed record StatusVinculoClienteOficinaDTO(
        int ClienteId,
        bool Ativo);
}
