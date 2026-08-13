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

        [Column("cor")]
        public string Cor { get; set; }

        [Column("id_cliente")]
        public int ClienteId { get; set; }

        public Cliente Cliente { get; set; }

        public ICollection<VeiculoImagem> Imagens { get; set; } = new List<VeiculoImagem>();
        public ICollection<RegistroServico> RegistroServicos { get; set; } = new List<RegistroServico>();
        public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();

        public Veiculo()
        {

        }
        public Veiculo(int id, string nomeVeiculo, string tipoVeiculo, string placaVeiculo, string chassiVeiculo, int anoFab, int quilometragem,
            string combustivel, string seguro, string cor)
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
            Cor = cor;
        }
    }
}
