using Microsoft.EntityFrameworkCore;
using SIGO.Data.Builders;
using SIGO.Objects.Models;

namespace SIGO.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Telefone> Telefones { get; set; }
        public DbSet<Servico> Servicos { get; set; }
        public DbSet<Marca> Marcas { get; set; }
        public DbSet<Veiculo> Veiculos { get; set; }
        public DbSet<Funcionario> Funcionarios { get; set; }
        public DbSet<Peca> Pecas { get; set; }
        public DbSet<Oficina> Oficinas { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<RegistroServico> RegistroServicos { get; set; }
        public DbSet<PecaSubstituida> PecasSubstituidas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ClienteBuilder.Build(modelBuilder);
            TelefoneBuilder.Build(modelBuilder);
            ServicoBuilder.Build(modelBuilder);
            MarcaBuilder.Build(modelBuilder);

            VeiculoBuilder.Build(modelBuilder);
            FuncionarioBuilder.Build(modelBuilder);
            PecaBuilder.Build(modelBuilder);
            OficinaBuilder.Build(modelBuilder);
            PedidoBuilder.Build(modelBuilder);
            RegistroServicoBuilder.Build(modelBuilder);


            modelBuilder.Entity<Funcionario_Servico>()
                .HasKey(fs => new { fs.IdFuncionario, fs.IdServico });

            modelBuilder.Entity<Funcionario_Servico>()
                .HasOne(fs => fs.Funcionario)
                .WithMany()
                .HasForeignKey(fs => fs.IdFuncionario);

            modelBuilder.Entity<Funcionario_Servico>()
                .HasOne(fs => fs.Servico)
                .WithMany(s => s.Funcionario_Servicos)
                .HasForeignKey(fs => fs.IdServico);

            modelBuilder.Entity<Pedido_Peca>()
                .HasKey(pp => new { pp.IdPedido, pp.IdPeca });

            modelBuilder.Entity<Pedido_Peca>()
                .HasOne(pp => pp.Pedido)
                .WithMany(p => p.Pedido_Pecas)
                .HasForeignKey(pp => pp.IdPedido);

            modelBuilder.Entity<Pedido_Peca>()
                .HasOne(pp => pp.Peca)
                .WithMany()
                .HasForeignKey(pp => pp.IdPeca);

            modelBuilder.Entity<Pedido_Servico>()
                .HasKey(ps => new { ps.IdPedido, ps.IdServico });

            modelBuilder.Entity<Pedido_Servico>()
                .HasOne(ps => ps.Pedido)
                .WithMany(p => p.Pedido_Servicos)
                .HasForeignKey(ps => ps.IdPedido);

            modelBuilder.Entity<Pedido_Servico>()
                .HasOne(ps => ps.Servico)
                .WithMany()
                .HasForeignKey(ps => ps.IdServico);
        }
    }
}
