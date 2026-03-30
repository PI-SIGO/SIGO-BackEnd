using SIGO.Objects.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIGO.Objects.Models
{
    [Table("veiculo")]
    public class Veiculo
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("nome")]
        public string NomeVeiculo { get; set; }

        [Column("tipo")]
        public string TipoVeiculo { get; set; }

        [Column("placa")]
        public string PlacaVeiculo { get; set; }

        [Column("chassi")]
        public string ChassiVeiculo { get; set; }

        [Column("ano")]
        public int AnoFab { get; set; }

        [Column("quilometragem")]
        public int Quilometragem { get; set; }

        [Column("combustivel")]
        public string Combustivel { get; set; }

        [Column("seguro")]
        public string Seguro { get; set; }

        [Column("status")]
        public Status Status { get; set; }

        [Column("id_cliente")]
        public int ClienteId { get; set; }

        public Cliente Cliente { get; set; }

        public ICollection<Cor> Cor { get; set; } = new List<Cor>();

        public ICollection<Marca> Marcas { get; set; } = new List<Marca>();

        public Veiculo()
        {

        }
        public Veiculo(int id, string nomeVeiculo, string tipoVeiculo, string placaVeiculo, string chassiVeiculo, int anoFab, int quilometragem,
            string combustivel, string seguro, Status status)
        {
            Id = id;
            NomeVeiculo = nomeVeiculo;
            TipoVeiculo = tipoVeiculo;
            PlacaVeiculo = placaVeiculo;
            ChassiVeiculo = chassiVeiculo;
            AnoFab = anoFab;
            Quilometragem = quilometragem;
            Combustivel = combustivel;
            Seguro = seguro;
            Status = status;
        }
    }
}
