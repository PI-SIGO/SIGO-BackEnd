namespace SIGO.Objects.Dtos.Entities
{
    public class ServicoDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public decimal Valor { get; set; }
        public DateOnly Garantia { get; set; }
        public int? IdOficina { get; set; }

        public List<Funcionario_ServicoDTO> Funcionario_Servicos { get; set; } = new();



    }
}
