using System.ComponentModel.DataAnnotations.Schema;

namespace SIGO.Objects.Models
{
    [Table("veiculo_imagem")]
    public class VeiculoImagem
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("id_veiculo")]
        public int VeiculoId { get; set; }

        [Column("url")]
        public string Url { get; set; }

        [Column("nome_arquivo")]
        public string NomeArquivo { get; set; }

        [Column("nome_original")]
        public string NomeOriginal { get; set; }

        [Column("content_type")]
        public string ContentType { get; set; }

        [Column("tamanho_bytes")]
        public long TamanhoBytes { get; set; }

        [Column("criado_em")]
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public Veiculo Veiculo { get; set; }
    }
}
