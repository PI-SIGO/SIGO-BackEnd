using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SIGO.Data;

#nullable disable

namespace SIGO.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260803201500_RecalculatePedidoTotalAsNet")]
    public partial class RecalculatePedidoTotalAsNet : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE pedido AS p
                SET "valorTotal" = GREATEST(
                    0::numeric,
                    COALESCE((
                        SELECT SUM(pp.valor_unitario * pp.quantidade)
                        FROM pedido_peca AS pp
                        WHERE pp.idpedido = p.id
                    ), 0) +
                    COALESCE((
                        SELECT SUM(ps.valor_unitario * ps."quantVezes")
                        FROM pedido_servico AS ps
                        WHERE ps."idPedido" = p.id
                    ), 0) - p."descontoTotalReais"
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE pedido AS p
                SET "valorTotal" =
                    COALESCE((
                        SELECT SUM(pp.valor_unitario * pp.quantidade)
                        FROM pedido_peca AS pp
                        WHERE pp.idpedido = p.id
                    ), 0) +
                    COALESCE((
                        SELECT SUM(ps.valor_unitario * ps."quantVezes")
                        FROM pedido_servico AS ps
                        WHERE ps."idPedido" = p.id
                    ), 0);
                """);
        }
    }
}
