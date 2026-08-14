using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGO.Migrations
{
    /// <inheritdoc />
    public partial class AddOpcoesCadastroView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE VIEW vw_opcoes_cadastro AS
                SELECT DISTINCT ON (id_oficina, categoria, LOWER(valor))
                    id_oficina,
                    categoria,
                    valor
                FROM (
                    SELECT
                        co.id_oficina,
                        'modeloVeiculo'::text AS categoria,
                        BTRIM(v.modelo) AS valor
                    FROM veiculo AS v
                    INNER JOIN cliente_oficina AS co
                        ON co.id_cliente = v.id_cliente
                    WHERE co.ativo = TRUE
                      AND co.revogado_em IS NULL

                    UNION ALL

                    SELECT
                        co.id_oficina,
                        'combustivel'::text AS categoria,
                        BTRIM(v.combustivel) AS valor
                    FROM veiculo AS v
                    INNER JOIN cliente_oficina AS co
                        ON co.id_cliente = v.id_cliente
                    WHERE co.ativo = TRUE
                      AND co.revogado_em IS NULL

                    UNION ALL

                    SELECT
                        co.id_oficina,
                        'cor'::text AS categoria,
                        BTRIM(v.cor) AS valor
                    FROM veiculo AS v
                    INNER JOIN cliente_oficina AS co
                        ON co.id_cliente = v.id_cliente
                    WHERE co.ativo = TRUE
                      AND co.revogado_em IS NULL

                    UNION ALL

                    SELECT
                        f.id_oficina,
                        'cargo'::text AS categoria,
                        BTRIM(f.cargo) AS valor
                    FROM funcionario AS f
                    WHERE f.id_oficina IS NOT NULL

                    UNION ALL

                    SELECT
                        p.id_oficina,
                        'tipoMarca'::text AS categoria,
                        BTRIM(m.tipomarca) AS valor
                    FROM peca AS p
                    INNER JOIN marca AS m
                        ON m.id = p.idmarca
                    WHERE p.id_oficina IS NOT NULL

                    UNION ALL

                    SELECT
                        p.id_oficina,
                        'fornecedor'::text AS categoria,
                        BTRIM(p.fornecedor) AS valor
                    FROM peca AS p
                    WHERE p.id_oficina IS NOT NULL
                ) AS opcoes
                WHERE valor IS NOT NULL
                  AND valor <> ''
                ORDER BY id_oficina, categoria, LOWER(valor), valor;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_opcoes_cadastro;");
        }
    }
}
