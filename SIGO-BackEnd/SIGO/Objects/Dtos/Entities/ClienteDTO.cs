using System.ComponentModel.DataAnnotations.Schema;

namespace SIGO.Objects.Dtos.Entities
{
    public class ClienteDTO
    {
        private string _email;
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Email
        {
            get => _email;
            set => _email = value.ToLower();
        }
        public string senha { get; set; }
        public string Cpf_Cnpj { get; set; }
        public string Obs { get; set; }
        public string razao { get; set; }
        public DateOnly DataNasc { get; set; }
        public int Numero { get; set; }
        public string Rua { get; set; }
        public string Cidade { get; set; }
        public string Cep { get; set; }
        public string Bairro { get; set; }
        public string Estado { get; set; }
        public string Pais { get; set; }
        public string Complemento { get; set; }

        public int Sexo { get; set; }
        public int TipoCliente { get; set; }
        public int Situacao { get; set; }

        public List<TelefoneDTO> Telefones { get; set; } = new();

        public List<VeiculoDTO> Veiculos { get; set; } = new();


    }
}
