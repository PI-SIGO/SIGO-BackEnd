namespace SIGO.Objects.Dtos.Entities
{
    public sealed record LoginClienteDTO
    {
        public string Cpf { get; init; } = string.Empty;
        public string Senha { get; init; } = string.Empty;
    }
}
