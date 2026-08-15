using Microsoft.EntityFrameworkCore;
using SIGO.Objects.Models;

namespace SIGO.Data.Builders
{
    public class AuditoriaFuncionarioBuilder
    {
        public static void Build(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditoriaFuncionario>()
                .HasKey(a => a.Id);

            modelBuilder.Entity<AuditoriaFuncionario>()
                .Property(a => a.FuncionarioId)
                .IsRequired();

            modelBuilder.Entity<AuditoriaFuncionario>()
                .Property(a => a.FuncionarioNome)
                .IsRequired()
                .HasMaxLength(150);

            modelBuilder.Entity<AuditoriaFuncionario>()
                .Property(a => a.Acao)
                .IsRequired()
                .HasMaxLength(30);

            modelBuilder.Entity<AuditoriaFuncionario>()
                .Property(a => a.Entidade)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<AuditoriaFuncionario>()
                .Property(a => a.EntidadeId);

            modelBuilder.Entity<AuditoriaFuncionario>()
                .Property(a => a.Descricao)
                .HasMaxLength(500);

            modelBuilder.Entity<AuditoriaFuncionario>()
                .Property(a => a.DataHora)
                .IsRequired();

            modelBuilder.Entity<AuditoriaFuncionario>()
                .HasIndex(a => a.FuncionarioId);

            modelBuilder.Entity<AuditoriaFuncionario>()
                .HasIndex(a => a.DataHora);
        }
    }
}