using System.ComponentModel.DataAnnotations.Schema;

namespace SIGO.Objects.Models
{
    [Table("compartilhamento_cliente")]
    public class CompartilhamentoCliente
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("id_cliente")]
        public int ClienteId { get; set; }

        public Cliente Cliente { get; set; }

        [Column("codigo_hash")]
        public string CodigoHash { get; set; }

        [Column("dados_permitidos")]
        public string DadosPermitidos { get; set; }

        [Column("expira_em")]
        public DateTime ExpiraEm { get; set; }

        [Column("usado_em")]
        public DateTime? UsadoEm { get; set; }

        [Column("ativo")]
        public bool Ativo { get; set; } = true;
    }
}
