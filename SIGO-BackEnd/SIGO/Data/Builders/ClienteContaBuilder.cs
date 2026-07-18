using Microsoft.EntityFrameworkCore;
using SIGO.Objects.Models;

namespace SIGO.Data.Builders
{
    public static class ClienteContaBuilder
    {
        public static void Build(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<ClienteConta>();

            entity.HasKey(c => c.Id);
            entity.Property(c => c.EmailNormalizado).IsRequired().HasMaxLength(254);
            entity.Property(c => c.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(c => c.Status).IsRequired();
            entity.Property(c => c.TokenVersion).IsRequired().HasDefaultValue(1);
            entity.Property(c => c.CreatedAt).IsRequired();
            entity.Property(c => c.UpdatedAt).IsRequired();

            entity.HasIndex(c => c.ClienteId)
                .IsUnique()
                .HasDatabaseName("IX_cliente_conta_id_cliente");

            entity.HasIndex(c => c.EmailNormalizado)
                .IsUnique()
                .HasDatabaseName("IX_cliente_conta_email_normalizado");

            entity.ToTable("cliente_conta", table =>
            {
                table.HasCheckConstraint("CK_cliente_conta_token_version", "token_version >= 1");
                table.HasCheckConstraint("CK_cliente_conta_status", "status BETWEEN 1 AND 2");
                table.HasCheckConstraint(
                    "CK_cliente_conta_email_normalizado",
                    "email_normalizado = lower(btrim(email_normalizado)) AND position('@' in email_normalizado) > 1");
            });

            entity.HasOne(c => c.Cliente)
                .WithOne(c => c.Conta)
                .HasForeignKey<ClienteConta>(c => c.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
