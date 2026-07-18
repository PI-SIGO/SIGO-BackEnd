namespace SIGO.Objects.Dtos.Entities
{
    public sealed record CadastrarClienteDTO
    {
        public string Cpf { get; init; } = string.Empty;
        public string Nome { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Senha { get; init; } = string.Empty;
    }

    public sealed record CadastroClienteResultadoDTO(
        int ClienteId,
        string Nome,
        string Email);
}
