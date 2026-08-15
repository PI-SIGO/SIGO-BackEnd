namespace SIGO.Objects.Dtos.Entities
{
    public class AuditoriaFuncionarioDTO
    {
        public int Id { get; set; }

        public int FuncionarioId { get; set; }

        public string FuncionarioNome { get; set; }

        public string Acao { get; set; }

        public string Entidade { get; set; }

        public int? EntidadeId { get; set; }

        public string? Descricao { get; set; }

        public DateTime DataHora { get; set; }
    }
}