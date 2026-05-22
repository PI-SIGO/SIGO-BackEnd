using Microsoft.EntityFrameworkCore;
using SIGO.Objects.Models;

namespace SIGO.Data.Builders
{
    public class VeiculoImagemBuilder
    {
        public static void Build(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<VeiculoImagem>().HasKey(i => i.Id);
            modelBuilder.Entity<VeiculoImagem>().Property(i => i.VeiculoId).IsRequired();
            modelBuilder.Entity<VeiculoImagem>().Property(i => i.Url).IsRequired().HasMaxLength(300);
            modelBuilder.Entity<VeiculoImagem>().Property(i => i.NomeArquivo).IsRequired().HasMaxLength(150);
            modelBuilder.Entity<VeiculoImagem>().Property(i => i.NomeOriginal).IsRequired().HasMaxLength(255);
            modelBuilder.Entity<VeiculoImagem>().Property(i => i.ContentType).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<VeiculoImagem>().Property(i => i.TamanhoBytes).IsRequired();
            modelBuilder.Entity<VeiculoImagem>().Property(i => i.CriadoEm).IsRequired();

            modelBuilder.Entity<VeiculoImagem>()
                .HasOne(i => i.Veiculo)
                .WithMany(v => v.Imagens)
                .HasForeignKey(i => i.VeiculoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VeiculoImagem>()
                .HasIndex(i => new { i.VeiculoId, i.CriadoEm })
                .HasDatabaseName("IX_veiculo_imagem_veiculo_criado_em");

            modelBuilder.Entity<VeiculoImagem>()
                .HasIndex(i => i.NomeArquivo)
                .IsUnique()
                .HasDatabaseName("IX_veiculo_imagem_nome_arquivo");
        }
    }
}
