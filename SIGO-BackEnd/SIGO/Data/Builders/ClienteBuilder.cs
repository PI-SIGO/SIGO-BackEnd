using Microsoft.EntityFrameworkCore;
using SIGO.Objects.Models;

namespace SIGO.Data.Builders
{
    public class ClienteBuilder
    {
        public static void Build(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Cliente>().HasKey(c => c.Id);
            modelBuilder.Entity<Cliente>().Property(c => c.Nome).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<Cliente>().Property(c => c.Email).IsRequired(false).HasMaxLength(254);
            modelBuilder.Entity<Cliente>().Property(c => c.Senha).IsRequired(false).HasMaxLength(100);
            modelBuilder.Entity<Cliente>().Property(c => c.Obs).HasMaxLength(500);
            modelBuilder.Entity<Cliente>().Property(c => c.Razao).HasMaxLength(500);
            modelBuilder.Entity<Cliente>().Property(c => c.Cpf_Cnpj).IsRequired().HasMaxLength(14);
            modelBuilder.Entity<Cliente>().Property(c => c.DataNasc);
            modelBuilder.Entity<Cliente>().Property(c => c.Numero).IsRequired();
            modelBuilder.Entity<Cliente>().Property(c => c.Rua).IsRequired(false).HasMaxLength(500);
            modelBuilder.Entity<Cliente>().Property(c => c.Cidade).IsRequired(false).HasMaxLength(500);
            modelBuilder.Entity<Cliente>().Property(c => c.Cep).IsRequired(false).HasMaxLength(8);
            modelBuilder.Entity<Cliente>().Property(c => c.Bairro).IsRequired(false).HasMaxLength(500);
            modelBuilder.Entity<Cliente>().Property(c => c.Estado).IsRequired(false).HasMaxLength(500);
            modelBuilder.Entity<Cliente>().Property(c => c.Pais).IsRequired(false).HasMaxLength(500);
            modelBuilder.Entity<Cliente>().Property(c => c.Complemento).IsRequired(false).HasMaxLength(500);

            modelBuilder.Entity<Cliente>().Property(c => c.Situacao).IsRequired();
            modelBuilder.Entity<Cliente>().Property(c => c.Sexo);
            modelBuilder.Entity<Cliente>().Property(c => c.TipoCliente).IsRequired();

            modelBuilder.Entity<Cliente>()
                .HasIndex(c => c.Cpf_Cnpj)
                .IsUnique()
                .HasDatabaseName("IX_cliente_cpf_cnpj");

            modelBuilder.Entity<Cliente>().ToTable("cliente", table =>
                table.HasCheckConstraint(
                    "CK_cliente_cpf_cnpj_normalizado",
                    "cpf_cnpj ~ '^[0-9]{11}$|^[0-9]{14}$'"));

            modelBuilder.Entity<Cliente>()
                .HasMany(c => c.Veiculos)
                .WithOne(v => v.Cliente)
                .HasForeignKey(v => v.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
