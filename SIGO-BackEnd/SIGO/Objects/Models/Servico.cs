using SIGO.Objects.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIGO.Objects.Models
{
    [Table("servico")]
    public class Servico
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("nome")]
        public string Nome { get; set; }

        [Column("descricao")]
        public string Descricao { get; set; }

        [Column("valor")]
        public decimal Valor { get; set; }

        [Column("garantia")]
        public DateOnly Garantia { get; set; }

        [Column("id_oficina")]
        public int? IdOficina { get; set; }
        public Oficina Oficina { get; set; }

        public ICollection<Funcionario_Servico> Funcionario_Servicos { get; set; } = new List<Funcionario_Servico>();

        public Servico()
        {
        }

        public Servico(int id, string nome, string descricao, decimal valor, DateOnly garantia, ICollection<Funcionario_Servico> funcionario_Servico)
        {
            Id = id;
            Nome = nome;
            Descricao = descricao;
            Valor = valor;
            Garantia = garantia;
            Funcionario_Servicos = funcionario_Servico;
        }
    }
}
