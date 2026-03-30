using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGO.Migrations
{
    /// <inheritdoc />
    public partial class concertandoapeca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "peca_marca");

            migrationBuilder.AddColumn<int>(
                name: "idmarca",
                table: "Peca",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Peca_idmarca",
                table: "Peca",
                column: "idmarca");

            migrationBuilder.AddForeignKey(
                name: "FK_Peca_marca_idmarca",
                table: "Peca",
                column: "idmarca",
                principalTable: "marca",
                principalColumn: "idMarca",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Peca_marca_idmarca",
                table: "Peca");

            migrationBuilder.DropIndex(
                name: "IX_Peca_idmarca",
                table: "Peca");

            migrationBuilder.DropColumn(
                name: "idmarca",
                table: "Peca");

            migrationBuilder.CreateTable(
                name: "peca_marca",
                columns: table => new
                {
                    id_peca = table.Column<int>(type: "integer", nullable: false),
                    id_marca = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_peca_marca", x => new { x.id_peca, x.id_marca });
                    table.ForeignKey(
                        name: "FK_peca_marca_Peca_id_peca",
                        column: x => x.id_peca,
                        principalTable: "Peca",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_peca_marca_marca_id_marca",
                        column: x => x.id_marca,
                        principalTable: "marca",
                        principalColumn: "idMarca",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_peca_marca_id_marca",
                table: "peca_marca",
                column: "id_marca");
        }
    }
}
