using Microsoft.EntityFrameworkCore;
using SIGO.Objects.Models;

namespace SIGO.Data.Builders
{
    public static class TokenRedefinicaoSenhaBuilder
    {
        public static void Build(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<TokenRedefinicaoSenha>();

            entity.HasKey(token => token.Id);
            entity.Property(token => token.TipoConta).IsRequired();
            entity.Property(token => token.ContaId).IsRequired();
            entity.Property(token => token.TokenHash)
                .IsRequired()
                .HasMaxLength(64)
                .IsFixedLength();
            entity.Property(token => token.ExpiraEm).IsRequired();
            entity.Property(token => token.CriadoEm).IsRequired();

            entity.HasIndex(token => token.TokenHash)
                .IsUnique()
                .HasDatabaseName("IX_token_redefinicao_senha_hash");

            entity.HasIndex(token => new
                {
                    token.TipoConta,
                    token.ContaId,
                    token.UsadoEm
                })
                .HasDatabaseName("IX_token_redefinicao_senha_conta_uso");

            entity.HasIndex(token => token.ExpiraEm)
                .HasDatabaseName("IX_token_redefinicao_senha_expiracao");

            entity.ToTable("token_redefinicao_senha", table =>
            {
                table.HasCheckConstraint(
                    "CK_token_redefinicao_senha_tipo_conta",
                    "tipo_conta BETWEEN 1 AND 3");
                table.HasCheckConstraint(
                    "CK_token_redefinicao_senha_expiracao",
                    "expira_em > criado_em");
            });
        }
    }
}
