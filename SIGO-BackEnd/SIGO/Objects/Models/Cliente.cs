using SIGO.Objects.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIGO.Objects.Models
{
    [Table("cliente")]
    public class Cliente
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("nome")]
        public string Nome { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("senha")]
        public string Senha { get; set; }

        [Column("cpf_cnpj")]
        public string Cpf_Cnpj { get; set; }

        [Column("obs")]
        public string Obs { get; set; }

        [Column("razao")]
        public string Razao { get; set; }

        [Column("datanasc")]
        public DateOnly? DataNasc { get; set; }

        [Column("sexo")]
        public Sexo Sexo { get; set; }

        [Column("numero")]
        public int Numero { get; set; }

        [Column("rua")]
        public string Rua { get; set; }

        [Column("cidade")]
        public string Cidade { get; set; }

        [Column("cep")]
        public string Cep { get; set; }

        [Column("bairro")]
        public string Bairro { get; set; }

        [Column("estado")]
        public string Estado { get; set; }

        [Column("pais")]
        public string Pais { get; set; }

        [Column("complemento")]
        public string Complemento { get; set; }

        [Column("tipocliente")]
        public TipoCliente TipoCliente { get; set; }

        [Column("situacao")]
        public Situacao Situacao { get; set; }

        public ICollection<Telefone> Telefones { get; set; } = new List<Telefone>();


        public Cliente()
        {

        }
        public Cliente(int id, string nome, string email, string senha, DateOnly data, Situacao situacao, string razao, Sexo sexo, TipoCliente tipoCliente,
            int numero, string rua, string cidade, string cep, string bairro, string estado, string pais)
        {
            Id = id;
            Nome = nome;
            Email = email;
            Senha = senha;
            Situacao = situacao;
            Razao = razao;
            Sexo = sexo;
            TipoCliente = tipoCliente;
            Numero = numero;
            Rua = rua;
            Cidade = cidade;
            Cep = cep;
            Bairro = bairro;
            Estado = estado;
            Pais = pais;
        }
    }
}
