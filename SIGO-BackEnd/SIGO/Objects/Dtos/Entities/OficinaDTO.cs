using SIGO.Objects.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIGO.Objects.Dtos.Entities
{
    public class OficinaDTO
    {
        public int Id { get; set; }

        public string Nome { get; set; }

        public string CNPJ { get; set; }

        public string Email { get; set; }

        public int Numero { get; set; }

        public string Rua { get; set; }

        public string Cidade { get; set; }

        public int Cep { get; set; }

        public string Bairro { get; set; }

        public string Estado { get; set; }

        public string Pais { get; set; }

        public string Complemento { get; set; }

        public string Senha { get; set; }

        public Situacao Situacao { get; set; }
    }
}
