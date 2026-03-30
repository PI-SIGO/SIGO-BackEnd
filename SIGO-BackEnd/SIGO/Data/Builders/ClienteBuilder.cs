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
            modelBuilder.Entity<Cliente>().Property(c => c.Email).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<Cliente>().Property(c => c.Senha).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<Cliente>().Property(c => c.Obs).HasMaxLength(500);
            modelBuilder.Entity<Cliente>().Property(c => c.Razao).HasMaxLength(500);
            modelBuilder.Entity<Cliente>().Property(c => c.Cpf_Cnpj).IsRequired().HasMaxLength(14);
            modelBuilder.Entity<Cliente>().Property(c => c.DataNasc);
            modelBuilder.Entity<Cliente>().Property(c => c.Numero).IsRequired();
            modelBuilder.Entity<Cliente>().Property(c => c.Rua).IsRequired().HasMaxLength(500);
            modelBuilder.Entity<Cliente>().Property(c => c.Cidade).IsRequired().HasMaxLength(500);
            modelBuilder.Entity<Cliente>().Property(c => c.Cep).IsRequired();
            modelBuilder.Entity<Cliente>().Property(c => c.Bairro).IsRequired().HasMaxLength(500);
            modelBuilder.Entity<Cliente>().Property(c => c.Estado).IsRequired().HasMaxLength(500);
            modelBuilder.Entity<Cliente>().Property(c => c.Pais).IsRequired().HasMaxLength(500);
            modelBuilder.Entity<Cliente>().Property(c => c.Complemento).IsRequired().HasMaxLength(500);

            modelBuilder.Entity<Cliente>().Property(c => c.Situacao).IsRequired();
            modelBuilder.Entity<Cliente>().Property(c => c.Sexo);
            modelBuilder.Entity<Cliente>().Property(c => c.TipoCliente).IsRequired();

            modelBuilder.Entity<Cliente>()
                .HasMany(c => c.Veiculos)
                .WithOne(v => v.Cliente)
                .HasForeignKey(v => v.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
