using System.ComponentModel.DataAnnotations.Schema;

namespace SIGO.Objects.Models
{
    [Table("pedido")]
    public class Pedido
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("id_cliente")]
        public int idCliente { get; set; }
        public Cliente Cliente { get; set; }

        [Column("id_funcionario")]
        public int idFuncionario { get; set; }
        public Funcionario Funcionario { get; set; }

        [Column("id_oficina")]
        public int idOficina { get; set; }
        public Oficina Oficina { get; set; }

        public ClienteOficina ClienteOficina { get; set; }

        [Column("id_veiculo")]
        public int idVeiculo { get; set; }
        public Veiculo Veiculo { get; set; }

        [Column("valorTotal")]
        public decimal ValorTotal { get; set; }

        [Column("descontoReais")]
        public decimal DescontoReais { get; set; }

        [Column("descontoPorcentagem")]
        public decimal DescontoPorcentagem { get; set; }

        [Column("descontoTotalReais")]
        public decimal DescontoTotalReais { get; set; }

        [Column("descontoServicoPorcentagem")]
        public decimal DescontoServicoPorcentagem { get; set; }

        [Column("descontoServicoReais")]
        public decimal DescontoServicoReais { get; set; }

        [Column("descontoPecaPorcentagem")]
        public decimal DescontoPecaPorcentagem { get; set; }

        [Column("descontoPecaReais")]
        public decimal descontoPecaReais { get; set; }

        [Column("observacao")]
        public string Observacao { get; set; }

        [Column("dataInicio")]
        public DateOnly DataInicio { get; set; }

        [Column("dataFim")]
        public DateOnly DataFim { get; set; }

        public ICollection<Pedido_Peca> Pedido_Pecas { get; set; } = new List<Pedido_Peca>();
        public ICollection<Pedido_Servico> Pedido_Servicos { get; set; } = new List<Pedido_Servico>();

        public Pedido()
        {
        }

        public Pedido(int id, int idCliente, Cliente cliente, int idFuncionario, Funcionario funcionario, int idOficina, Oficina oficina, int idVeiculo, Veiculo veiculo, decimal valorTotal, decimal descontoReais, decimal descontoPorcentagem, decimal descontoTotalReais, decimal descontoServicoPorcentagem, decimal descontoServicoReais, decimal descontoPecaPorcentagem, decimal descontoPecaReais, string observacao, DateOnly dataInicio, DateOnly dataFim, ICollection<Pedido_Peca> pedido_Pecas, ICollection<Pedido_Servico> pedido_Servicos)
        {
            Id = id;
            this.idCliente = idCliente;
            Cliente = cliente;
            this.idFuncionario = idFuncionario;
            Funcionario = funcionario;
            this.idOficina = idOficina;
            Oficina = oficina;
            this.idVeiculo = idVeiculo;
            Veiculo = veiculo;
            ValorTotal = valorTotal;
            DescontoReais = descontoReais;
            DescontoPorcentagem = descontoPorcentagem;
            DescontoTotalReais = descontoTotalReais;
            DescontoServicoPorcentagem = descontoServicoPorcentagem;
            DescontoServicoReais = descontoServicoReais;
            DescontoPecaPorcentagem = descontoPecaPorcentagem;
            this.descontoPecaReais = descontoPecaReais;
            Observacao = observacao;
            DataInicio = dataInicio;
            DataFim = dataFim;
            Pedido_Pecas = pedido_Pecas;
            Pedido_Servicos = pedido_Servicos;
        }
    }
}
