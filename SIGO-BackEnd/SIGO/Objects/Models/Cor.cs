using System.ComponentModel.DataAnnotations.Schema;

namespace SIGO.Objects.Models
{
    [Table("cor")]
    public class Cor
    {
        [Column("cor")]
        public int Id { get; set; }
        [Column("nome")]
        public string NomeCor { get; set; }
        public Cor()
        {
        }
        public Cor(int id, string nomeCor)
        {
            Id = id;
            NomeCor = nomeCor;
        }
    }
}
