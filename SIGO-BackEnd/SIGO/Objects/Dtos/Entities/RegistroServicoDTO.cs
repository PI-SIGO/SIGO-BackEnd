namespace SIGO.Objects.Dtos.Entities
{
    public class RegistroServicoDTO
    {
        public int Id { get; set; }

        public int VeiculoId { get; set; }

        public int? ServicoId { get; set; }

        public ServicoDTO? Servico { get; set; }

        public DateTime DataServico { get; set; }

        public string Descricao { get; set; }

        public int Quilometragem { get; set; }

        public string Responsavel { get; set; }

        public ICollection<PecaSubstituidaDTO> PecasSubstituidas { get; set; } = new List<PecaSubstituidaDTO>();
    }
}
