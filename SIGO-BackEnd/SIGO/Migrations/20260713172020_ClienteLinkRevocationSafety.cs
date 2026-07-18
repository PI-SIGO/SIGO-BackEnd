using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGO.Migrations
{
    /// <inheritdoc />
    public partial class ClienteLinkRevocationSafety : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "revogado_em",
                table: "cliente_oficina",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE cliente_oficina
                SET revogado_em = updated_at
                WHERE ativo = FALSE
                  AND revogado_em IS NULL;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_cliente_oficina_ativo_revogado",
                table: "cliente_oficina",
                sql: "NOT (ativo AND revogado_em IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_cliente_oficina_ativo_revogado",
                table: "cliente_oficina");

            migrationBuilder.DropColumn(
                name: "revogado_em",
                table: "cliente_oficina");
        }
    }
}
