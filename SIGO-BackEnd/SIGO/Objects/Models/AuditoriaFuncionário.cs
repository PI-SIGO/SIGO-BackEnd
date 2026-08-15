using System.ComponentModel.DataAnnotations.Schema;

namespace SIGO.Objects.Models
{
    [Table("auditoria_funcionario")]
    public class AuditoriaFuncionario
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("funcionario_id")]
        public int FuncionarioId { get; set; }

        [Column("funcionario_nome")]
        public string FuncionarioNome { get; set; }

        [Column("acao")]
        public string Acao { get; set; }

        [Column("entidade")]
        public string Entidade { get; set; }

        [Column("entidade_id")]
        public int? EntidadeId { get; set; }

        [Column("descricao")]
        public string? Descricao { get; set; }

        [Column("data_hora")]
        public DateTime DataHora { get; set; }
    }
}