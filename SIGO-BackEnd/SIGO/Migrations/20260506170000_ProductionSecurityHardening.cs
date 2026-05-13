using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SIGO.Data;

#nullable disable

namespace SIGO.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260506170000_ProductionSecurityHardening")]
    public partial class ProductionSecurityHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "role",
                table: "funcionario",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Funcionario");

            migrationBuilder.AddColumn<int>(
                name: "id_oficina",
                table: "servico",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "id_oficina",
                table: "peca",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_servico_id_oficina",
                table: "servico",
                column: "id_oficina");

            migrationBuilder.CreateIndex(
                name: "IX_peca_id_oficina",
                table: "peca",
                column: "id_oficina");

            migrationBuilder.AddForeignKey(
                name: "FK_peca_oficina_id_oficina",
                table: "peca",
                column: "id_oficina",
                principalTable: "oficina",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_servico_oficina_id_oficina",
                table: "servico",
                column: "id_oficina",
                principalTable: "oficina",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_peca_oficina_id_oficina",
                table: "peca");

            migrationBuilder.DropForeignKey(
                name: "FK_servico_oficina_id_oficina",
                table: "servico");

            migrationBuilder.DropIndex(
                name: "IX_servico_id_oficina",
                table: "servico");

            migrationBuilder.DropIndex(
                name: "IX_peca_id_oficina",
                table: "peca");

            migrationBuilder.DropColumn(
                name: "role",
                table: "funcionario");

            migrationBuilder.DropColumn(
                name: "id_oficina",
                table: "servico");

            migrationBuilder.DropColumn(
                name: "id_oficina",
                table: "peca");
        }
    }
}
