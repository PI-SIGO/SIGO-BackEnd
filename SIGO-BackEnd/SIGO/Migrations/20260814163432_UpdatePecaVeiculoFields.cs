using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGO.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePecaVeiculoFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tipo",
                table: "peca");

            migrationBuilder.RenameColumn(
                name: "tipo",
                table: "veiculo",
                newName: "modelo");

            migrationBuilder.AlterColumn<string>(
                name: "chassi",
                table: "veiculo",
                type: "character varying(17)",
                maxLength: 17,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(17)",
                oldMaxLength: 17);

            migrationBuilder.AddColumn<string>(
                name: "EAN",
                table: "peca",
                type: "character varying(13)",
                maxLength: 13,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EAN",
                table: "peca");

            migrationBuilder.RenameColumn(
                name: "modelo",
                table: "veiculo",
                newName: "tipo");

            migrationBuilder.AlterColumn<string>(
                name: "chassi",
                table: "veiculo",
                type: "character varying(17)",
                maxLength: 17,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(17)",
                oldMaxLength: 17,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tipo",
                table: "peca",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
