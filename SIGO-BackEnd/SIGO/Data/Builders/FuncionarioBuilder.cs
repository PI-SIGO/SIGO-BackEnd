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
            modelBuilder.Entity<Funcionario>().Property(f => f.Situacao).IsRequired();

        }
    }
}
