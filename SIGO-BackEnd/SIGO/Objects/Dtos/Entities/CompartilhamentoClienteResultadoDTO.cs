namespace SIGO.Objects.Dtos.Entities
{
    public class CompartilhamentoClienteResultadoDTO
    {
        public int ClienteId { get; set; }
        public Dictionary<string, object?> Dados { get; set; } = new();
    }
}
