using Microsoft.EntityFrameworkCore;
using SIGO.Objects.Models;

namespace SIGO.Data.Builders
{
    public class ServicoBuilder
    {
        public static void Build(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Servico>().HasKey(c => c.Id);
            modelBuilder.Entity<Servico>().Property(c => c.Nome).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<Servico>().Property(c => c.Garantia);
            modelBuilder.Entity<Servico>().Property(c => c.Valor).IsRequired().HasPrecision(18, 2);
            modelBuilder.Entity<Servico>().Property(c => c.Descricao).IsRequired();
            modelBuilder.Entity<Servico>().Property(c => c.IdOficina);

            modelBuilder.Entity<Servico>()
                .HasIndex(s => new { s.IdOficina, s.Nome })
                .HasDatabaseName("IX_servico_id_oficina_nome");

            modelBuilder.Entity<Servico>()
                .HasOne(s => s.Oficina)
                .WithMany()
                .HasForeignKey(s => s.IdOficina)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
