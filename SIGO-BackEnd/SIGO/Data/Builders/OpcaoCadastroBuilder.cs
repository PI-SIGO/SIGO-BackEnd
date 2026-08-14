using Microsoft.EntityFrameworkCore;
using SIGO.Objects.Models;

namespace SIGO.Data.Builders
{
    public static class OpcaoCadastroBuilder
    {
        public static void Build(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OpcaoCadastro>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("vw_opcoes_cadastro");
                entity.Property(option => option.IdOficina).HasColumnName("id_oficina");
                entity.Property(option => option.Categoria).HasColumnName("categoria");
                entity.Property(option => option.Valor).HasColumnName("valor");
            });
        }
    }
}
