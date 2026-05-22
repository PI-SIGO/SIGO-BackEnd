using SIGO.Objects.Enums;

namespace SIGO.Objects.Dtos.Entities
{
    public class VeiculoDTO
    {
        public int Id { get; set; }
        public string NomeVeiculo { get; set; }
        public string TipoVeiculo { get; set; }
        public string PlacaVeiculo { get; set; }
        public string ChassiVeiculo { get; set; }
        public int AnoFab { get; set; }
        public int Quilometragem { get; set; }
        public string Combustivel { get; set; }
        public string Seguro { get; set; }
        public string Cor { get; set; }
        public int ClienteId { get; set; }
        public Situacao Situacao { get; set; }
        public ICollection<VeiculoImagemDTO> Imagens { get; set; } = new List<VeiculoImagemDTO>();
    }
}
