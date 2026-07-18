namespace SIGO.Objects.Dtos.Entities
{
    public sealed record VeiculoRequestDTO
    {
        public string NomeVeiculo { get; init; } = string.Empty;
        public string TipoVeiculo { get; init; } = string.Empty;
        public string PlacaVeiculo { get; init; } = string.Empty;
        public string ChassiVeiculo { get; init; } = string.Empty;
        public int AnoFab { get; init; }
        public int Quilometragem { get; init; }
        public string Combustivel { get; init; } = string.Empty;
        public string Seguro { get; init; } = string.Empty;
        public string Cor { get; init; } = string.Empty;
    }
}
