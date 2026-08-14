using SIGO.Objects.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIGO.Objects.Dtos.Entities
{
    public class PecaDTO
    {
        public int Id { get; set; }

        public string Nome { get; set; }

        public string Descricao { get; set; }

        public decimal Valor { get; set; }

        public int Quantidade { get; set; }

        public int QuantidadeEstoque { get; set; }

        public string EAN { get; set; }

        public DateOnly Garantia { get; set; }

        public int Unidade { get; set; }

        public int IdMarca { get; set; }

        public DateOnly DataAquisicao { get; set; }

        public string Fornecedor { get; set; }

        public int? IdOficina { get; set; }
    }
}
