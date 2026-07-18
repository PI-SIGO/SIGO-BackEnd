using SIGO.Objects.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIGO.Objects.Models
{
    [Table("cliente_contato")]
    public class ClienteContato
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("id_cliente")]
        public int ClienteId { get; set; }

        public Cliente Cliente { get; set; }

        [Column("tipo")]
        public TipoContatoCliente Tipo { get; set; }

        [Column("valor_normalizado")]
        public string ValorNormalizado { get; set; }

        [Column("origem")]
        public OrigemContatoCliente Origem { get; set; }

        [Column("verificado_em")]
        public DateTime? VerificadoEm { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
