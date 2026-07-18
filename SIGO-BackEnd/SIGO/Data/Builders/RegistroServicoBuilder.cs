using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
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
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Oficina)
                    .WithMany()
                    .HasForeignKey(r => r.OficinaId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();

                entity.Property(r => r.OficinaId)
                    .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

                entity.HasOne(r => r.Servico)
                    .WithMany()
                    .HasForeignKey(r => r.ServicoId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(r => r.PecasSubstituidas)
                    .WithOne(p => p.RegistroServico)
                    .HasForeignKey(p => p.RegistroServicoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(r => new { r.VeiculoId, r.DataServico })
                    .IsDescending(false, true)
                    .HasDatabaseName("IX_registro_servico_veiculo_data");

                entity.HasIndex(r => new { r.OficinaId, r.DataServico })
                    .IsDescending(false, true)
                    .HasDatabaseName("IX_registro_servico_oficina_data");
            });

            modelBuilder.Entity<PecaSubstituida>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Id).HasColumnName("id");
            });
        }
    }
}
