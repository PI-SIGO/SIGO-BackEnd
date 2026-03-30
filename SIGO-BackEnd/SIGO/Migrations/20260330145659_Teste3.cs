using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGO.Migrations
{
    /// <inheritdoc />
    public partial class Teste3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cores_veiculo_VeiculoId",
                table: "Cores");

            migrationBuilder.DropForeignKey(
                name: "FK_Peca_marca_idmarca",
                table: "Peca");

            migrationBuilder.DropForeignKey(
                name: "FK_pedido_peca_Peca_idpeca",
                table: "pedido_peca");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Peca",
                table: "Peca");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Cores",
                table: "Cores");

            migrationBuilder.RenameTable(
                name: "Peca",
                newName: "peca");

            migrationBuilder.RenameTable(
                name: "Cores",
                newName: "cor");

            migrationBuilder.RenameIndex(
                name: "IX_Peca_idmarca",
                table: "peca",
                newName: "IX_peca_idmarca");

            migrationBuilder.RenameColumn(
                name: "NomeCor",
                table: "cor",
                newName: "nome");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "cor",
                newName: "cor");

            migrationBuilder.RenameIndex(
                name: "IX_Cores_VeiculoId",
                table: "cor",
                newName: "IX_cor_VeiculoId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_peca",
                table: "peca",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_cor",
                table: "cor",
                column: "cor");

            migrationBuilder.AddForeignKey(
                name: "FK_cor_veiculo_VeiculoId",
                table: "cor",
                column: "VeiculoId",
                principalTable: "veiculo",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_peca_marca_idmarca",
                table: "peca",
                column: "idmarca",
                principalTable: "marca",
                principalColumn: "idMarca",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_pedido_peca_peca_idpeca",
                table: "pedido_peca",
                column: "idpeca",
                principalTable: "peca",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cor_veiculo_VeiculoId",
                table: "cor");

            migrationBuilder.DropForeignKey(
                name: "FK_peca_marca_idmarca",
                table: "peca");

            migrationBuilder.DropForeignKey(
                name: "FK_pedido_peca_peca_idpeca",
                table: "pedido_peca");

            migrationBuilder.DropPrimaryKey(
                name: "PK_peca",
                table: "peca");

            migrationBuilder.DropPrimaryKey(
                name: "PK_cor",
                table: "cor");

            migrationBuilder.RenameTable(
                name: "peca",
                newName: "Peca");

            migrationBuilder.RenameTable(
                name: "cor",
                newName: "Cores");

            migrationBuilder.RenameIndex(
                name: "IX_peca_idmarca",
                table: "Peca",
                newName: "IX_Peca_idmarca");

            migrationBuilder.RenameColumn(
                name: "nome",
                table: "Cores",
                newName: "NomeCor");

            migrationBuilder.RenameColumn(
                name: "cor",
                table: "Cores",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_cor_VeiculoId",
                table: "Cores",
                newName: "IX_Cores_VeiculoId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Peca",
                table: "Peca",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Cores",
                table: "Cores",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Cores_veiculo_VeiculoId",
                table: "Cores",
                column: "VeiculoId",
                principalTable: "veiculo",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Peca_marca_idmarca",
                table: "Peca",
                column: "idmarca",
                principalTable: "marca",
                principalColumn: "idMarca",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_pedido_peca_Peca_idpeca",
                table: "pedido_peca",
                column: "idpeca",
                principalTable: "Peca",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
