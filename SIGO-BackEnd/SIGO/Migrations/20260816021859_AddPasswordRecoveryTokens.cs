using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SIGO.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordRecoveryTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "token_redefinicao_senha",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tipo_conta = table.Column<int>(type: "integer", nullable: false),
                    conta_id = table.Column<int>(type: "integer", nullable: false),
                    token_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    expira_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    usado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_token_redefinicao_senha", x => x.id);
                    table.CheckConstraint("CK_token_redefinicao_senha_expiracao", "expira_em > criado_em");
                    table.CheckConstraint("CK_token_redefinicao_senha_tipo_conta", "tipo_conta BETWEEN 1 AND 3");
                });

            migrationBuilder.CreateIndex(
                name: "IX_token_redefinicao_senha_conta_uso",
                table: "token_redefinicao_senha",
                columns: new[] { "tipo_conta", "conta_id", "usado_em" });

            migrationBuilder.CreateIndex(
                name: "IX_token_redefinicao_senha_expiracao",
                table: "token_redefinicao_senha",
                column: "expira_em");

            migrationBuilder.CreateIndex(
                name: "IX_token_redefinicao_senha_hash",
                table: "token_redefinicao_senha",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "token_redefinicao_senha");
        }
    }
}
