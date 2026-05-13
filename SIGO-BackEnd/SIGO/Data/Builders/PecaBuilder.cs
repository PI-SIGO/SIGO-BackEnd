using Microsoft.EntityFrameworkCore;
using SIGO.Objects.Models;

namespace SIGO.Data.Builders
{
    public class PecaBuilder
    {
        public static void Build(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Peca>().HasKey(p => p.Id);
            modelBuilder.Entity<Peca>().Property(p => p.Nome).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<Peca>().Property(p => p.Tipo).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<Peca>().Property(p => p.Descricao).HasMaxLength(500);
            modelBuilder.Entity<Peca>().Property(p => p.Valor).IsRequired().HasPrecision(18, 2);
            modelBuilder.Entity<Peca>().Property(p => p.Quantidade).IsRequired();
            modelBuilder.Entity<Peca>().Property(p => p.Garantia).IsRequired();
            modelBuilder.Entity<Peca>().Property(p => p.Unidade).IsRequired();
            modelBuilder.Entity<Peca>().Property(p => p.IdOficina);
            modelBuilder.Entity<Peca>().Property(p => p.DataAquisicao).IsRequired();
            modelBuilder.Entity<Peca>().Property(p => p.Fornecedor).IsRequired().HasMaxLength(100);

            modelBuilder.Entity<Peca>()
                .HasIndex(p => new { p.IdOficina, p.Nome })
                .HasDatabaseName("IX_peca_id_oficina_nome");

            modelBuilder.Entity<Peca>()
                .HasOne(p => p.Oficina)
                .WithMany()
                .HasForeignKey(p => p.IdOficina)
                .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
