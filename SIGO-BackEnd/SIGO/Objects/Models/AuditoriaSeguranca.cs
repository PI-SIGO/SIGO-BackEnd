using SIGO.Objects.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIGO.Objects.Models
{
    [Table("auditoria_seguranca")]
    public class AuditoriaSeguranca
    {
        [Column("id")]
        public long Id { get; set; }

        [Column("id_cliente")]
        public int? ClienteId { get; set; }

        public Cliente? Cliente { get; set; }

        [Column("tipo_ator")]
        public TipoAtorAuditoria TipoAtor { get; set; }

        [Column("id_ator")]
        public int? AtorId { get; set; }

        [Column("evento")]
        public TipoEventoAuditoria Evento { get; set; }

        [Column("resultado")]
        public ResultadoAuditoria Resultado { get; set; }

        [Column("documento_hash")]
        public string? DocumentoHash { get; set; }

        [Column("contato_hash")]
        public string? ContatoHash { get; set; }

        [Column("documento_mascarado")]
        public string? DocumentoMascarado { get; set; }

        [Column("contato_mascarado")]
        public string? ContatoMascarado { get; set; }

        [Column("ip_address")]
        public string? IpAddress { get; set; }

        [Column("correlation_id")]
        public string? CorrelationId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
