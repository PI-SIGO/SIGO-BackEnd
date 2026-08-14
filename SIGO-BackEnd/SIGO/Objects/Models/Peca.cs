using System.ComponentModel.DataAnnotations.Schema;

namespace SIGO.Objects.Models
{
    [Table("peca")]
    public class Peca
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("nome")]
        public string Nome { get; set; }

        [Column("descricao")]
        public string Descricao { get; set; }
    
        [Column("valor")]
        public decimal Valor { get; set; }

        [Column("quantidade")]
        public int Quantidade { get; set; }

        [Column("quantidade_estoque")]
        public int QuantidadeEstoque { get; set; }

        [Column("garantia")]
        public DateOnly Garantia { get; set; }

        [Column("unidade")]
        public int Unidade { get; set; }

        [Column("EAN")]
        public string EAN { get; set; }

        [Column("idmarca")]
        public int IdMarca { get; set; }
        public Marca Marca { get; set; }

        [Column("dataAquisicao")]
        public DateOnly DataAquisicao { get; set; }

        [Column("fornecedor")]
        public string Fornecedor { get; set; }

        [Column("id_oficina")]
        public int? IdOficina { get; set; }
        public Oficina Oficina { get; set; }

        public Peca() { }
    
        public Peca(int id, string nome, string descricao, decimal valor, int quantidade, DateOnly garantia,
            int unidade, DateOnly dataAquisicao, string fornecedor, int quantidadeEstoque = 0)
        {
            Id = id;
            Nome = nome;
            Descricao = descricao;
            Valor = valor;
            Quantidade = quantidade;
            QuantidadeEstoque = quantidadeEstoque;
            Garantia = garantia;
            Unidade = unidade;
            DataAquisicao = dataAquisicao;
            Fornecedor = fornecedor;
        }
    }
}
