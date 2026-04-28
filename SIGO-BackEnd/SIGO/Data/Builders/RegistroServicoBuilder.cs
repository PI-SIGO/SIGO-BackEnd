using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SIGO.Objects.Models;

namespace SIGO.Data.Builders
{
    public static class RegistroServicoBuilder
    {
        public static void Build(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RegistroServico>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Id).HasColumnName("id");

                entity.HasOne(r => r.Veiculo)
                    .WithMany(v => v.RegistroServicos)
                    .HasForeignKey(r => r.VeiculoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Servico)
                    .WithMany()
                    .HasForeignKey(r => r.ServicoId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(r => r.PecasSubstituidas)
                    .WithOne(p => p.RegistroServico)
                    .HasForeignKey(p => p.RegistroServicoId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PecaSubstituida>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Id).HasColumnName("id");
            });
        }
    }
}
