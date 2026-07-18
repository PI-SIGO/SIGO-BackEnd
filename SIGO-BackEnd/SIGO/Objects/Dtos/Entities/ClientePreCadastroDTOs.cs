using SIGO.Objects.Enums;

namespace SIGO.Objects.Dtos.Entities
{
    public sealed record PreCadastroClienteResultadoDTO(
        int ClienteId,
        string Nome,
        string Cpf,
        bool VinculoAtivo);

    public sealed record VinculoClienteOficinaResumoDTO(
        int OficinaId,
        string OficinaNome,
        bool Ativo,
        DateTime CriadoEm,
        DateTime? RevogadoEm);

    public sealed record PreCadastrarClienteDTO
    {
        public string Cpf { get; init; } = string.Empty;
        public string Nome { get; init; } = string.Empty;
        public string? Email { get; init; }

        // Mantido para compatibilidade com o contrato inicial. Novos clientes
        // devem preferir a coleção estruturada em Telefones.
        public string? Telefone { get; init; }

        public string? Obs { get; init; }
        public string? Razao { get; init; }
        public DateOnly? DataNasc { get; init; }
        public Sexo? Sexo { get; init; }
        public int? Numero { get; init; }
        public string? Rua { get; init; }
        public string? Cidade { get; init; }
        public string? Cep { get; init; }
        public string? Bairro { get; init; }
        public string? Estado { get; init; }
        public string? Pais { get; init; }
        public string? Complemento { get; init; }
        public IReadOnlyCollection<PreCadastrarTelefoneClienteDTO> Telefones { get; init; } =
            Array.Empty<PreCadastrarTelefoneClienteDTO>();
    }

    public sealed record PreCadastrarTelefoneClienteDTO
    {
        public int DDD { get; init; }
        public string Numero { get; init; } = string.Empty;
    }

    public sealed record AlterarSenhaClienteDTO
    {
        public string SenhaAtual { get; init; } = string.Empty;
        public string NovaSenha { get; init; } = string.Empty;
    }
}
