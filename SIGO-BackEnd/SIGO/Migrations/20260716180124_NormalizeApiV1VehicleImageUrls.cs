using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGO.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeApiV1VehicleImageUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE veiculo_imagem
                SET url = REPLACE(url, '/api/veiculos/', '/api/v1/veiculos/')
                WHERE url LIKE '/api/veiculos/%';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE veiculo_imagem
                SET url = REPLACE(url, '/api/v1/veiculos/', '/api/veiculos/')
                WHERE url LIKE '/api/v1/veiculos/%';
                """);
        }
    }
}
