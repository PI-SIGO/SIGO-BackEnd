using System.Text.Json.Serialization;

namespace SIGO.Objects.Dtos.Entities
{
    public sealed record CadastrarClienteDTO
    {
        public string? Cpf { get; init; }
        public string? Cpf_Cnpj { get; init; }
        public string Nome { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Senha { get; init; } = string.Empty;

        [JsonIgnore]
        public string Documento =>
            string.IsNullOrWhiteSpace(Cpf_Cnpj) ? Cpf ?? string.Empty : Cpf_Cnpj;
    }

    public sealed record CadastroClienteResultadoDTO(
        int ClienteId,
        string Nome,
        string Email);
}
