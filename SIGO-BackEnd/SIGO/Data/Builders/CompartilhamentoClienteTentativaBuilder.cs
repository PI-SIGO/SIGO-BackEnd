using Microsoft.EntityFrameworkCore;
using SIGO.Objects.Models;

namespace SIGO.Data.Builders
{
    public class CompartilhamentoClienteTentativaBuilder
    {
        public static void Build(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CompartilhamentoClienteTentativa>().HasKey(t => t.Id);
            modelBuilder.Entity<CompartilhamentoClienteTentativa>().Property(t => t.CodigoHash).IsRequired().HasMaxLength(128);
            modelBuilder.Entity<CompartilhamentoClienteTentativa>().Property(t => t.IpAddress).HasMaxLength(64);
            modelBuilder.Entity<CompartilhamentoClienteTentativa>().Property(t => t.Motivo).IsRequired().HasMaxLength(64);
            modelBuilder.Entity<CompartilhamentoClienteTentativa>().Property(t => t.TentadoEm).IsRequired();

            modelBuilder.Entity<CompartilhamentoClienteTentativa>()
                .HasOne(t => t.Oficina)
                .WithMany()
                .HasForeignKey(t => t.OficinaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CompartilhamentoClienteTentativa>()
                .HasIndex(t => new { t.OficinaId, t.IpAddress, t.TentadoEm });
        }
    }
}
