using SIGO.Objects.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIGO.Objects.Models
{
    [Table("cliente_conta")]
    public class ClienteConta
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("id_cliente")]
        public int ClienteId { get; set; }

        public Cliente Cliente { get; set; }

        [Column("email_normalizado")]
        public string EmailNormalizado { get; set; }

        [Column("password_hash")]
        public string PasswordHash { get; set; }

        [Column("status")]
        public EstadoClienteConta Status { get; set; }

        [Column("token_version")]
        public int TokenVersion { get; set; } = 1;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
