using System.ComponentModel.DataAnnotations.Schema;

namespace SIGO.Objects.Models
{
    [Table("pedido_peca")]
    public class Pedido_Peca
    {
        [Column("idpedido")]
        public int IdPedido { get; set; }
        public Pedido Pedido { get; set; }

        [Column("idpeca")]
        public int IdPeca { get; set; }
        public Peca Peca { get; set; }

        [Column("quantidade")]
        public int Quantidade { get; set; }

        [Column("valor_unitario")]
        public decimal ValorUnitario { get; set; }

        [Column("datainstalacao")]
        public DateOnly DataInstalacao { get; set; }

        [Column("estado")]
        public string Estado { get; set; }

        [Column("observacao")]
        public string Observacao { get; set; }

        public Pedido_Peca()
        {
        }

         public Pedido_Peca(int idPedido, int idPeca, int quantidade, decimal valorUnitario, DateOnly dataInstalacao, string estado, string observacao)
        {
            IdPedido = idPedido;
            IdPeca = idPeca;
            Quantidade = quantidade;
            ValorUnitario = valorUnitario;
            DataInstalacao = dataInstalacao;
            Estado = estado;
            Observacao = observacao;
        }
    }
}
