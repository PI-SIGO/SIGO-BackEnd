using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SIGO.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditoriaFuncionario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "auditoria_funcionario",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    funcionario_id = table.Column<int>(type: "integer", nullable: false),
                    funcionario_nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    acao = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    entidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entidade_id = table.Column<int>(type: "integer", nullable: true),
                    descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    data_hora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auditoria_funcionario", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_auditoria_funcionario_data_hora",
                table: "auditoria_funcionario",
                column: "data_hora");

            migrationBuilder.CreateIndex(
                name: "IX_auditoria_funcionario_funcionario_id",
                table: "auditoria_funcionario",
                column: "funcionario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auditoria_funcionario");
        }
    }
}
