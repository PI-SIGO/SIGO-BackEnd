using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SIGO.Data;

#nullable disable

namespace SIGO.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260527170500_FullAccessClienteOficinaVinculo")]
    public partial class FullAccessClienteOficinaVinculo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE cliente_oficina
                    ALTER COLUMN dados_permitidos
                    SET DEFAULT '["Nome","Email","Cpf_Cnpj","Telefones","Veiculos"]';

                UPDATE cliente_oficina
                SET dados_permitidos = '["Nome","Email","Cpf_Cnpj","Telefones","Veiculos"]',
                    updated_at = now()
                WHERE ativo = TRUE;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE cliente_oficina
                    ALTER COLUMN dados_permitidos
                    SET DEFAULT '["Nome"]';
                """);
        }
    }
}
