using SIGO.Objects.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIGO.Objects.Dtos.Entities
{
    public class PecaDTO //n liga
    {
        public int Id { get; set; }

        public string Nome { get; set; }

        public string Tipo { get; set; }

        public string Descricao { get; set; }

        public float Valor { get; set; }

        public int Quantidade { get; set; }

        public DateOnly Garantia { get; set; }

        public int Unidade { get; set; }

        public DateOnly DataAquisicao { get; set; }

        public string Fornecedor { get; set; }

        public List<Marca> Marcas { get; set; } = new List<Marca>();
    }
}
