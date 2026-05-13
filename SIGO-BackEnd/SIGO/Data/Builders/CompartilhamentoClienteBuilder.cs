using Microsoft.EntityFrameworkCore;
using SIGO.Objects.Models;

namespace SIGO.Data.Builders
{
    public class CompartilhamentoClienteBuilder
    {
        public static void Build(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CompartilhamentoCliente>().HasKey(c => c.Id);
            modelBuilder.Entity<CompartilhamentoCliente>().Property(c => c.CodigoHash).IsRequired().HasMaxLength(128);
            modelBuilder.Entity<CompartilhamentoCliente>().Property(c => c.DadosPermitidos).IsRequired();
            modelBuilder.Entity<CompartilhamentoCliente>().Property(c => c.ExpiraEm).IsRequired();
            modelBuilder.Entity<CompartilhamentoCliente>().Property(c => c.Ativo).IsRequired();

            modelBuilder.Entity<CompartilhamentoCliente>()
                .HasOne(c => c.Cliente)
                .WithMany()
                .HasForeignKey(c => c.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CompartilhamentoCliente>()
                .HasIndex(c => c.CodigoHash)
                .IsUnique()
                .HasFilter("ativo AND usado_em IS NULL");
        }
    }
}
