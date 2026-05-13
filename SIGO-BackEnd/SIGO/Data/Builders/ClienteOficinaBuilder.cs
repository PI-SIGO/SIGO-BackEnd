using Microsoft.EntityFrameworkCore;
using SIGO.Objects.Models;

namespace SIGO.Data.Builders
{
    public class ClienteOficinaBuilder
    {
        public static void Build(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ClienteOficina>().HasKey(co => new { co.OficinaId, co.ClienteId });
            modelBuilder.Entity<ClienteOficina>().Property(co => co.Ativo).IsRequired();
            modelBuilder.Entity<ClienteOficina>().Property(co => co.DadosPermitidos).IsRequired();
            modelBuilder.Entity<ClienteOficina>().Property(co => co.CreatedAt).IsRequired();
            modelBuilder.Entity<ClienteOficina>().Property(co => co.UpdatedAt).IsRequired();

            modelBuilder.Entity<ClienteOficina>()
                .HasOne(co => co.Oficina)
                .WithMany(o => o.ClienteOficinas)
                .HasForeignKey(co => co.OficinaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClienteOficina>()
                .HasOne(co => co.Cliente)
                .WithMany(c => c.ClienteOficinas)
                .HasForeignKey(co => co.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClienteOficina>()
                .HasIndex(co => co.ClienteId);

            modelBuilder.Entity<ClienteOficina>()
                .HasIndex(co => new { co.OficinaId, co.Ativo, co.ClienteId })
                .HasDatabaseName("IX_cliente_oficina_oficina_ativo_cliente");
        }
    }
}
