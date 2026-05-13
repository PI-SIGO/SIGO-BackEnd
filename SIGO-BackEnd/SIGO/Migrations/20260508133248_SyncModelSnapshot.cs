using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGO.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_servico_id_oficina",
                table: "servico");

            migrationBuilder.DropIndex(
                name: "IX_registro_servico_id_veiculo",
                table: "registro_servico");

            migrationBuilder.DropIndex(
                name: "IX_peca_id_oficina",
                table: "peca");

            migrationBuilder.DropIndex(
                name: "IX_funcionario_id_oficina",
                table: "funcionario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_servico_id_oficina",
                table: "servico",
                column: "id_oficina");

            migrationBuilder.CreateIndex(
                name: "IX_registro_servico_id_veiculo",
                table: "registro_servico",
                column: "id_veiculo");

            migrationBuilder.CreateIndex(
                name: "IX_peca_id_oficina",
                table: "peca",
                column: "id_oficina");

            migrationBuilder.CreateIndex(
                name: "IX_funcionario_id_oficina",
                table: "funcionario",
                column: "id_oficina");
        }
    }
}
