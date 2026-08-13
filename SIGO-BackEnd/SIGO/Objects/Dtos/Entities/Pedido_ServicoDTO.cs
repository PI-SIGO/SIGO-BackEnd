namespace SIGO.Objects.Dtos.Entities
{
    public class Pedido_ServicoDTO
    {
        public int IdPedido { get; set; }

        public int IdServico { get; set; }

        public string NomeServico { get; internal set; }

        public int QuantVezes { get; set; }

        public decimal ValorUnitario { get; set; }

        public decimal Subtotal => ValorUnitario * QuantVezes;
    }
}
