using SIGO.Objects.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIGO.Objects.Models
{
    [Table("oficina")]
    public class Oficina
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("nome")]
        public string Nome { get; set; }

        [Column("cnpj")]
        public string CNPJ { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("numero")]
        public int Numero { get; set; }

        [Column("rua")]
        public string Rua { get; set; }

        [Column("cidade")]
        public string Cidade { get; set; }

        [Column("cep")]
        public int Cep { get; set; }

        [Column("bairro")]
        public string Bairro { get; set; }

        [Column("estado")]
        public string Estado { get; set; }

        [Column("pais")]
        public string Pais { get; set; }

        [Column("complemento")]
        public string Complemento { get; set; }

        [Column("senha")]
        public string Senha { get; set; }

        [Column("situacao")]
        public Situacao Situacao { get; set; }

        public ICollection<Telefone> Telefones { get; set; } = new List<Telefone>();

        public Oficina() { }

        public Oficina(int id, string nome, string cnpj, string email, int numero, string rua, string cidade, int cep, string bairro,
            string estado, string pais, string complemento, Situacao situacao)
        {
            Id = id;
            Nome = nome;
            CNPJ = cnpj;
            Email = email;
            Numero = numero;
            Rua = rua;
            Cidade = cidade;
            Cep = cep;
            Bairro = bairro;
            Estado = estado;
            Pais = pais;
            Complemento = complemento;
            Situacao = situacao;
        }
    }
}
