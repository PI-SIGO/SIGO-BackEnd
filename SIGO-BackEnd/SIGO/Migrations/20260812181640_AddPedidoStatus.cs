using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGO.Migrations
{
    /// <inheritdoc />
    public partial class AddPedidoStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "pedido",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "status",
                table: "pedido");
        }
    }
}
