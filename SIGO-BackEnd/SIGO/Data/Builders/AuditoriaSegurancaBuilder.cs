using Microsoft.EntityFrameworkCore;
using SIGO.Objects.Models;

namespace SIGO.Data.Builders
{
    public static class AuditoriaSegurancaBuilder
    {
        public static void Build(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<AuditoriaSeguranca>();

            entity.HasKey(a => a.Id);
            entity.Property(a => a.TipoAtor).IsRequired();
            entity.Property(a => a.Evento).IsRequired();
            entity.Property(a => a.Resultado).IsRequired();
            entity.Property(a => a.DocumentoHash).HasMaxLength(64);
            entity.Property(a => a.ContatoHash).HasMaxLength(64);
            entity.Property(a => a.DocumentoMascarado).HasMaxLength(32);
            entity.Property(a => a.ContatoMascarado).HasMaxLength(254);
            entity.Property(a => a.IpAddress).HasMaxLength(64);
            entity.Property(a => a.CorrelationId).HasMaxLength(128);
            entity.Property(a => a.CreatedAt).IsRequired();

            entity.HasIndex(a => new { a.Evento, a.CreatedAt })
                .HasDatabaseName("IX_auditoria_seguranca_evento_created_at");
            entity.HasIndex(a => a.DocumentoHash)
                .HasDatabaseName("IX_auditoria_seguranca_documento_hash");

            entity.HasOne(a => a.Cliente)
                .WithMany(c => c.AuditoriasSeguranca)
                .HasForeignKey(a => a.ClienteId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.ToTable("auditoria_seguranca", table =>
            {
                table.HasCheckConstraint("CK_auditoria_seguranca_tipo_ator", "tipo_ator BETWEEN 0 AND 5");
                table.HasCheckConstraint(
                    "CK_auditoria_seguranca_evento",
                    "evento IN (2, 4, 5, 6, 7, 8, 9)");
                table.HasCheckConstraint("CK_auditoria_seguranca_resultado", "resultado BETWEEN 1 AND 2");
            });
        }
    }
}
