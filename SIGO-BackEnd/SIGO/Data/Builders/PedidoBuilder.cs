using Microsoft.EntityFrameworkCore;
using SIGO.Objects.Models;

namespace SIGO.Data.Builders
{
    public class PedidoBuilder
    {
        public static void Build(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Pedido>().HasKey(p => p.Id);

            modelBuilder.Entity<Pedido>().Property(p => p.ValorTotal).IsRequired().HasPrecision(18, 2);
            modelBuilder.Entity<Pedido>().Property(p => p.DescontoReais).IsRequired().HasPrecision(18, 2);
            modelBuilder.Entity<Pedido>().Property(p => p.DescontoPorcentagem).IsRequired().HasPrecision(5, 2);
            modelBuilder.Entity<Pedido>().Property(p => p.DescontoTotalReais).IsRequired().HasPrecision(18, 2);
            modelBuilder.Entity<Pedido>().Property(p => p.DescontoServicoPorcentagem).IsRequired().HasPrecision(5, 2);
            modelBuilder.Entity<Pedido>().Property(p => p.DescontoServicoReais).IsRequired().HasPrecision(18, 2);
            modelBuilder.Entity<Pedido>().Property(p => p.DescontoPecaPorcentagem).IsRequired().HasPrecision(5, 2);
            modelBuilder.Entity<Pedido>().Property(p => p.descontoPecaReais).IsRequired().HasPrecision(18, 2);
            modelBuilder.Entity<Pedido>().Property(p => p.Observacao).HasMaxLength(500);
            modelBuilder.Entity<Pedido>().Property(p => p.DataInicio).IsRequired();
            modelBuilder.Entity<Pedido>().Property(p => p.DataFim).IsRequired();

            modelBuilder.Entity<Pedido>()
                .HasIndex(p => new { p.idVeiculo, p.DataInicio })
                .IsDescending(false, true)
                .HasDatabaseName("IX_pedido_id_veiculo_dataInicio");

            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Cliente)
                .WithMany()
                .HasForeignKey(p => p.idCliente)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Funcionario)
                .WithMany()
                .HasForeignKey(p => p.idFuncionario)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Oficina)
                .WithMany()
                .HasForeignKey(p => p.idOficina)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Veiculo)
                .WithMany()
                .HasForeignKey(p => new { p.idVeiculo, p.idCliente })
                .HasPrincipalKey(v => new { v.Id, v.ClienteId })
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.ClienteOficina)
                .WithMany(co => co.Pedidos)
                .HasForeignKey(p => new { p.idOficina, p.idCliente })
                .HasPrincipalKey(co => new { co.OficinaId, co.ClienteId })
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
