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

        [Column("tipo")]
        public string Tipo { get; set; }

        [Column("descricao")]
        public string Descricao { get; set; }
    
        [Column("valor")]
        public float Valor { get; set; }

        [Column("quantidade")]
        public int Quantidade { get; set; }

        [Column("garantia")]
        public DateOnly Garantia { get; set; }

        [Column("unidade")]
        public int Unidade { get; set; }

        [Column("idmarca")]
        public int IdMarca { get; set; }
        public Marca Marca { get; set; }

        [Column("dataAquisicao")]
        public DateOnly DataAquisicao { get; set; }

        [Column("fornecedor")]
        public string Fornecedor { get; set; }

        public Peca() { }
    
        public Peca(int id, string nome, string tipo, string descricao, float valor, int quantidade, DateOnly garantia, 
            int unidade, DateOnly dataAquisicao, string fornecedor)
        {
            Id = id;
            Nome = nome;
            Tipo = tipo;
            Descricao = descricao;
            Valor = valor;
            Quantidade = quantidade;
            Garantia = garantia;
            Unidade = unidade;
            DataAquisicao = dataAquisicao;
            Fornecedor = fornecedor;
        }
    }
}
