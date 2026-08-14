namespace SIGO.Objects.Dtos.Entities
{
    public sealed record OpcoesCadastroDTO(
        IReadOnlyList<string> ModelosVeiculo,
        IReadOnlyList<string> Combustiveis,
        IReadOnlyList<string> Cores,
        IReadOnlyList<string> Cargos,
        IReadOnlyList<string> TiposMarca,
        IReadOnlyList<string> Fornecedores);
}
