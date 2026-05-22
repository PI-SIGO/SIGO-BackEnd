namespace SIGO.Objects.Dtos.Entities
{
    public class VeiculoImagemDTO
    {
        public int Id { get; set; }
        public int VeiculoId { get; set; }
        public string Url { get; set; }
        public string NomeOriginal { get; set; }
        public string ContentType { get; set; }
        public long TamanhoBytes { get; set; }
        public DateTime CriadoEm { get; set; }
    }
}
