using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGO.Migrations
{
    /// <inheritdoc />
    public partial class RemoveInvalidVehicleBrandRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_marca_veiculo_VeiculoId",
                table: "marca");

            migrationBuilder.DropIndex(
                name: "IX_marca_VeiculoId",
                table: "marca");

            migrationBuilder.DropColumn(
                name: "VeiculoId",
                table: "marca");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VeiculoId",
                table: "marca",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_marca_VeiculoId",
                table: "marca",
                column: "VeiculoId");

            migrationBuilder.AddForeignKey(
                name: "FK_marca_veiculo_VeiculoId",
                table: "marca",
                column: "VeiculoId",
                principalTable: "veiculo",
                principalColumn: "id");
        }
    }
}
