using Microsoft.EntityFrameworkCore;
using SIGO.Objects.Models;

namespace SIGO.Data.Builders
{
    public static class ClienteContatoBuilder
    {
        public static void Build(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<ClienteContato>();

            entity.HasKey(c => c.Id);
            entity.Property(c => c.Tipo).IsRequired();
            entity.Property(c => c.ValorNormalizado).IsRequired().HasMaxLength(254);
            entity.Property(c => c.Origem).IsRequired();
            entity.Property(c => c.CreatedAt).IsRequired();

            entity.HasIndex(c => new { c.ClienteId, c.Tipo, c.ValorNormalizado })
                .IsUnique()
                .HasDatabaseName("IX_cliente_contato_cliente_tipo_valor");

            entity.HasIndex(c => new { c.Tipo, c.ValorNormalizado })
                .HasDatabaseName("IX_cliente_contato_tipo_valor");

            entity.HasOne(c => c.Cliente)
                .WithMany(c => c.Contatos)
                .HasForeignKey(c => c.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable("cliente_contato", table =>
            {
                table.HasCheckConstraint("CK_cliente_contato_tipo", "tipo BETWEEN 1 AND 2");
                table.HasCheckConstraint("CK_cliente_contato_origem", "origem BETWEEN 1 AND 3");
                table.HasCheckConstraint(
                    "CK_cliente_contato_valor_normalizado",
                    "(tipo = 1 AND valor_normalizado = lower(btrim(valor_normalizado)) AND position('@' in valor_normalizado) > 1) OR " +
                    "(tipo = 2 AND valor_normalizado ~ '^[0-9]{10,13}$')");
            });
        }
    }
}
