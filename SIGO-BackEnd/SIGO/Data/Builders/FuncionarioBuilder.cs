using Microsoft.EntityFrameworkCore;
using SIGO.Objects.Models;

namespace SIGO.Data.Builders
{
    public class FuncionarioBuilder
    {
        public static void Build(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Funcionario>().HasKey(f => f.Id);
            modelBuilder.Entity<Funcionario>().Property(f => f.Nome).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<Funcionario>().Property(f => f.Cargo).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<Funcionario>().Property(f => f.Cpf).IsRequired().HasMaxLength(12);
            modelBuilder.Entity<Funcionario>().Property(f => f.Email).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<Funcionario>().Property(f => f.Senha).IsRequired();   
            modelBuilder.Entity<Funcionario>().Property(f => f.Role).IsRequired().HasMaxLength(30).HasDefaultValue(SIGO.Security.SystemRoles.Funcionario);
            modelBuilder.Entity<Funcionario>().Property(f => f.Situacao).IsRequired();
            modelBuilder.Entity<Funcionario>().Property(f => f.IdOficina);

            modelBuilder.Entity<Funcionario>()
                .HasIndex(f => f.Cpf)
                .IsUnique()
                .HasDatabaseName("IX_funcionario_cpf");

            modelBuilder.Entity<Funcionario>()
                .HasIndex(f => f.Email)
                .IsUnique()
                .HasDatabaseName("IX_funcionario_email");

            modelBuilder.Entity<Funcionario>()
                .HasIndex(f => new { f.IdOficina, f.Nome })
                .HasDatabaseName("IX_funcionario_id_oficina_nome");

            modelBuilder.Entity<Funcionario>()
                .HasOne(f => f.Oficina)
                .WithMany(o => o.Funcionarios)
                .HasForeignKey(f => f.IdOficina)
                .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
