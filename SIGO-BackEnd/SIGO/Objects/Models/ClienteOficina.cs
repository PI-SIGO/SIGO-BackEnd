using System.ComponentModel.DataAnnotations.Schema;

namespace SIGO.Objects.Models
{
    [Table("cliente_oficina")]
    public class ClienteOficina
    {
        [Column("id_oficina")]
        public int OficinaId { get; set; }

        public Oficina Oficina { get; set; }

        [Column("id_cliente")]
        public int ClienteId { get; set; }

        public Cliente Cliente { get; set; }

        [Column("ativo")]
        public bool Ativo { get; set; } = true;

        [Column("dados_permitidos")]
        public string DadosPermitidos { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
    }
}
