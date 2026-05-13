using Microsoft.EntityFrameworkCore;
using SIGO.Objects.Models;

namespace SIGO.Data.Builders
{
    public class OficinaBuilder
    {
        public static void Build(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Oficina>().HasKey(o => o.Id);
            modelBuilder.Entity<Oficina>().Property(o => o.Nome).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<Oficina>().Property(o => o.CNPJ).IsRequired();
            modelBuilder.Entity<Oficina>().Property(o => o.Email).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<Oficina>().Property(o => o.Numero).IsRequired();
            modelBuilder.Entity<Oficina>().Property(o => o.Rua).IsRequired().HasMaxLength(200);
            modelBuilder.Entity<Oficina>().Property(o => o.Cidade).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<Oficina>().Property(o => o.Cep).IsRequired();
            modelBuilder.Entity<Oficina>().Property(o => o.Bairro).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<Oficina>().Property(o => o.Estado).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<Oficina>().Property(o => o.Pais).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<Oficina>().Property(o => o.Complemento).HasMaxLength(200);
            modelBuilder.Entity<Oficina>().Property(o => o.Senha).IsRequired();
            modelBuilder.Entity<Oficina>().Property(o => o.Situacao).IsRequired();

            modelBuilder.Entity<Oficina>()
                .HasIndex(o => o.CNPJ)
                .IsUnique()
                .HasDatabaseName("IX_oficina_cnpj");

            modelBuilder.Entity<Oficina>()
                .HasIndex(o => o.Email)
                .IsUnique()
                .HasDatabaseName("IX_oficina_email");
        }
    }
}
