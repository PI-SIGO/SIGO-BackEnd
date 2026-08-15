using System.Text.Json.Serialization;

namespace SIGO.Objects.Dtos.Entities
{
    public sealed record LoginClienteDTO
    {
        public string? Cpf { get; init; }
        public string? Cpf_Cnpj { get; init; }
        public string Senha { get; init; } = string.Empty;

        [JsonIgnore]
        public string Documento =>
            string.IsNullOrWhiteSpace(Cpf_Cnpj) ? Cpf ?? string.Empty : Cpf_Cnpj;
    }
}
