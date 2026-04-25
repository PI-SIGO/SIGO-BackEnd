using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace SIGO.Objects.Models
{
    [Table("registro_servico")]
    public class RegistroServico
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("id_veiculo")]
        public int VeiculoId { get; set; }
        public Veiculo Veiculo { get; set; }

        [Column("id_servico")]
        public int? ServicoId { get; set; }
        public Servico? Servico { get; set; }

        [Column("data_servico")]
        public DateTime DataServico { get; set; }

        [Column("descricao")]
        public string Descricao { get; set; }

        [Column("quilometragem")]
        public int Quilometragem { get; set; }

        [Column("responsavel")]
        public string Responsavel { get; set; }

        public ICollection<PecaSubstituida> PecasSubstituidas { get; set; } = new List<PecaSubstituida>();
    }
}
