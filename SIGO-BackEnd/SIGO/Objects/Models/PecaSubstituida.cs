using System.ComponentModel.DataAnnotations.Schema;

namespace SIGO.Objects.Models
{
    [Table("peca_substituida")]
    public class PecaSubstituida
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("id_registro_servico")]
        public int RegistroServicoId { get; set; }
        public RegistroServico RegistroServico { get; set; }

        [Column("nome")]
        public string Nome { get; set; }

        [Column("quantidade")]
        public int Quantidade { get; set; }

        [Column("observacao")]
        public string Observacao { get; set; }
    }
}
