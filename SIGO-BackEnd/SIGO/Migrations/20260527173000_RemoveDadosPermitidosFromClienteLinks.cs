using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SIGO.Data;

#nullable disable

namespace SIGO.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260527173000_RemoveDadosPermitidosFromClienteLinks")]
    public partial class RemoveDadosPermitidosFromClienteLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE cliente_oficina
                    DROP COLUMN IF EXISTS dados_permitidos;

                ALTER TABLE compartilhamento_cliente
                    DROP COLUMN IF EXISTS dados_permitidos;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE cliente_oficina
                    ADD COLUMN IF NOT EXISTS dados_permitidos text NOT NULL DEFAULT '["Nome","Email","Cpf_Cnpj","Telefones","Veiculos"]';

                ALTER TABLE compartilhamento_cliente
                    ADD COLUMN IF NOT EXISTS dados_permitidos text NOT NULL DEFAULT '["Nome","Email","Cpf_Cnpj","Telefones","Veiculos"]';
                """);
        }
    }
}
