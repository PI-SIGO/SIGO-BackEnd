using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SIGO.Data;

#nullable disable

namespace SIGO.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260803190000_AddPedidoItemPriceSnapshots")]
    public partial class AddPedidoItemPriceSnapshots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "valor_unitario",
                table: "pedido_servico",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "valor_unitario",
                table: "pedido_peca",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(
                """
                UPDATE pedido_servico AS ps
                SET valor_unitario = s.valor
                FROM servico AS s
                WHERE ps."idServico" = s.id;

                UPDATE pedido_peca AS pp
                SET valor_unitario = p.valor
                FROM peca AS p
                WHERE pp.idpeca = p.id;

                ALTER TABLE pedido_servico ALTER COLUMN valor_unitario DROP DEFAULT;
                ALTER TABLE pedido_peca ALTER COLUMN valor_unitario DROP DEFAULT;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "valor_unitario",
                table: "pedido_servico");

            migrationBuilder.DropColumn(
                name: "valor_unitario",
                table: "pedido_peca");
        }
    }
}
