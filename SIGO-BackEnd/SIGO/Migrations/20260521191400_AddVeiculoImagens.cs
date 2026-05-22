using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SIGO.Migrations
{
    /// <inheritdoc />
    public partial class AddVeiculoImagens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "veiculo_imagem",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_veiculo = table.Column<int>(type: "integer", nullable: false),
                    url = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    nome_arquivo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    nome_original = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tamanho_bytes = table.Column<long>(type: "bigint", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_veiculo_imagem", x => x.id);
                    table.ForeignKey(
                        name: "FK_veiculo_imagem_veiculo_id_veiculo",
                        column: x => x.id_veiculo,
                        principalTable: "veiculo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_veiculo_imagem_nome_arquivo",
                table: "veiculo_imagem",
                column: "nome_arquivo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_veiculo_imagem_veiculo_criado_em",
                table: "veiculo_imagem",
                columns: new[] { "id_veiculo", "criado_em" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "veiculo_imagem");
        }
    }
}
