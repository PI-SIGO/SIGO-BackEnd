using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGO.Migrations
{
    /// <inheritdoc />
    public partial class Teste2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM (
                            SELECT regexp_replace(coalesce(cpf_cnpj, ''), '\D', '', 'g') AS documento
                            FROM cliente
                        ) normalizados
                        WHERE documento <> ''
                        GROUP BY documento
                        HAVING count(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Não foi possível criar índice único em cliente.cpf_cnpj: existem CPF/CNPJ duplicados após normalização.';
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM (
                            SELECT lower(trim(coalesce(email, ''))) AS email_normalizado
                            FROM cliente
                        ) normalizados
                        WHERE email_normalizado <> ''
                        GROUP BY email_normalizado
                        HAVING count(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Não foi possível criar índice único em cliente.email: existem e-mails duplicados após normalização.';
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "IX_cliente_cpf_cnpj_normalizado"
                ON cliente (regexp_replace(coalesce(cpf_cnpj, ''), '\D', '', 'g'));
                """);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "IX_cliente_email_normalizado"
                ON cliente (lower(trim(coalesce(email, ''))));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_cliente_cpf_cnpj_normalizado";
                """);

            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_cliente_email_normalizado";
                """);
        }
    }
}
