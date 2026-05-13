namespace SIGO.Objects.Dtos.Entities
{
    public class PedidoDTO
    {
        public int Id { get; set; }

        public int idCliente { get; set; }

        public int idFuncionario { get; set; }

        public int idOficina { get; set; }

        public int idVeiculo { get; set; }

        public decimal ValorTotal { get; set; }

        public decimal DescontoReais { get; set; }

        public decimal DescontoPorcentagem { get; set; }

        public decimal DescontoTotalReais { get; set; }

        public decimal DescontoServicoPorcentagem { get; set; }

        public decimal DescontoServicoReais { get; set; }

        public decimal DescontoPecaPorcentagem { get; set; }

        public decimal descontoPecaReais { get; set; }

        public string Observacao { get; set; }

        public DateOnly DataInicio { get; set; }

        public DateOnly DataFim { get; set; }

        public List<Pedido_PecaDTO> Pedido_Pecas { get; set; } = new();
        public List<Pedido_ServicoDTO> Pedido_Servicos { get; set; } = new();
    }
}
