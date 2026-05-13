using System.ComponentModel.DataAnnotations.Schema;

namespace SIGO.Objects.Models
{
    [Table("compartilhamento_cliente_tentativa")]
    public class CompartilhamentoClienteTentativa
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("id_oficina")]
        public int OficinaId { get; set; }

        [Column("codigo_hash")]
        public string CodigoHash { get; set; } = string.Empty;

        [Column("ip_address")]
        public string? IpAddress { get; set; }

        [Column("sucesso")]
        public bool Sucesso { get; set; }

        [Column("motivo")]
        public string Motivo { get; set; } = string.Empty;

        [Column("tentado_em")]
        public DateTime TentadoEm { get; set; } = DateTime.UtcNow;

        public Oficina Oficina { get; set; }
    }
}
