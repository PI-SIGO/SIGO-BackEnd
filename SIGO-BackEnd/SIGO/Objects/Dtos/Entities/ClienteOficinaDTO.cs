namespace SIGO.Objects.Dtos.Entities
{
    public class ClienteOficinaDTO
    {
        public int Id { get; set; }
        public string? Nome { get; set; }
        public string? Email { get; set; }
        public string? Cpf_Cnpj { get; set; }
        public List<TelefoneDTO>? Telefones { get; set; }
        public List<VeiculoDTO>? Veiculos { get; set; }
    }
}
