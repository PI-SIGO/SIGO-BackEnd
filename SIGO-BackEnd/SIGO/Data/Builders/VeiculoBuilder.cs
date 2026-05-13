using Microsoft.EntityFrameworkCore;
using SIGO.Objects.Models;

namespace SIGO.Data.Builders
{
    public class VeiculoBuilder
    {
        public static void Build(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Veiculo>().HasKey(v => v.Id);
            modelBuilder.Entity<Veiculo>().Property(v => v.NomeVeiculo).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<Veiculo>().Property(v => v.TipoVeiculo).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<Veiculo>().Property(v => v.PlacaVeiculo).IsRequired().HasMaxLength(8);
            modelBuilder.Entity<Veiculo>().Property(v => v.ChassiVeiculo).IsRequired().HasMaxLength(17);
            modelBuilder.Entity<Veiculo>().Property(v => v.AnoFab).IsRequired();
            modelBuilder.Entity<Veiculo>().Property(v => v.Quilometragem).IsRequired();
            modelBuilder.Entity<Veiculo>().Property(v => v.Combustivel).IsRequired().HasMaxLength(30);
            modelBuilder.Entity<Veiculo>().Property(v => v.Seguro).HasMaxLength(100);
            modelBuilder.Entity<Veiculo>().Property(v => v.Status).IsRequired();
            modelBuilder.Entity<Veiculo>().Property(v => v.ClienteId).IsRequired();
            modelBuilder.Entity<Veiculo>().Property(v => v.Cor).IsRequired();

            modelBuilder.Entity<Veiculo>()
                .HasAlternateKey(v => new { v.Id, v.ClienteId });
        }
    }
}
