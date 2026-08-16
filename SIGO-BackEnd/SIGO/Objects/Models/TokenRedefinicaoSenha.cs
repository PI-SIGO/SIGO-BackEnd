using SIGO.Objects.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIGO.Objects.Models
{
    [Table("token_redefinicao_senha")]
    public sealed class TokenRedefinicaoSenha
    {
        [Column("id")]
        public long Id { get; set; }

        [Column("tipo_conta")]
        public TipoContaRecuperacao TipoConta { get; set; }

        [Column("conta_id")]
        public int ContaId { get; set; }

        [Column("token_hash")]
        public string TokenHash { get; set; } = string.Empty;

        [Column("expira_em")]
        public DateTime ExpiraEm { get; set; }

        [Column("usado_em")]
        public DateTime? UsadoEm { get; set; }

        [Column("criado_em")]
        public DateTime CriadoEm { get; set; }
    }
}
