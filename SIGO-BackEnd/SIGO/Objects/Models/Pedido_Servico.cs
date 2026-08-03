using System.ComponentModel.DataAnnotations.Schema;

namespace SIGO.Objects.Models
{
    [Table("pedido_servico")]
    public class Pedido_Servico
    {
        [Column("idPedido")]
        public int IdPedido { get; set; }
        public Pedido Pedido { get; set; }

        [Column("idServico")]
        public int IdServico { get; set; }
        public Servico Servico { get; set; }

        [Column("quantVezes")]
        public int QuantVezes { get; set; }

        [Column("valor_unitario")]
        public decimal ValorUnitario { get; set; }

        public Pedido_Servico()
        {
        }

         public Pedido_Servico(int idPedido, int idServico, int quantVezes, decimal valorUnitario)
        {
            IdPedido = idPedido;
            IdServico = idServico;
            QuantVezes = quantVezes;
            ValorUnitario = valorUnitario;
        }
    }
}
