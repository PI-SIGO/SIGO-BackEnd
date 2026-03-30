using Microsoft.EntityFrameworkCore;
using SIGO.Objects.Models;

namespace SIGO.Data.Builders
{
    public class MarcaBuilder
    {
        public static void Build(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Marca>().HasKey(m => m.IdMarca);
            modelBuilder.Entity<Marca>().Property(m => m.NomeMarca).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<Marca>().Property(m => m.DescMarca).HasMaxLength(500);
            modelBuilder.Entity<Marca>().Property(m => m.TipoMarca).IsRequired().HasMaxLength(50);

            modelBuilder.Entity<Marca>()
                .HasMany(p => p.Pecas)
                .WithOne(m => m.Marca)
                .HasForeignKey(v => v.IdMarca)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}