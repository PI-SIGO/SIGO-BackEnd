using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGO.Migrations
{
    /// <inheritdoc />
    public partial class FixPedidoClienteOficinaFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS cliente_oficina (
                    id_oficina integer NOT NULL,
                    id_cliente integer NOT NULL,
                    ativo boolean NOT NULL DEFAULT TRUE,
                    dados_permitidos text NOT NULL DEFAULT '["Nome","Email","Cpf_Cnpj","Telefones","Veiculos"]',
                    created_at timestamp with time zone NOT NULL DEFAULT now(),
                    updated_at timestamp with time zone NOT NULL DEFAULT now()
                );

                ALTER TABLE cliente_oficina
                    ADD COLUMN IF NOT EXISTS ativo boolean NOT NULL DEFAULT TRUE,
                    ADD COLUMN IF NOT EXISTS dados_permitidos text NOT NULL DEFAULT '["Nome","Email","Cpf_Cnpj","Telefones","Veiculos"]',
                    ADD COLUMN IF NOT EXISTS created_at timestamp with time zone NOT NULL DEFAULT now(),
                    ADD COLUMN IF NOT EXISTS updated_at timestamp with time zone NOT NULL DEFAULT now();

                DO $migration$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'PK_cliente_oficina'
                    ) THEN
                        ALTER TABLE cliente_oficina
                            ADD CONSTRAINT "PK_cliente_oficina" PRIMARY KEY (id_oficina, id_cliente);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_cliente_oficina_cliente_id_cliente'
                    ) THEN
                        ALTER TABLE cliente_oficina
                            ADD CONSTRAINT "FK_cliente_oficina_cliente_id_cliente"
                            FOREIGN KEY (id_cliente) REFERENCES cliente (id) ON DELETE CASCADE;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_cliente_oficina_oficina_id_oficina'
                    ) THEN
                        ALTER TABLE cliente_oficina
                            ADD CONSTRAINT "FK_cliente_oficina_oficina_id_oficina"
                            FOREIGN KEY (id_oficina) REFERENCES oficina (id) ON DELETE CASCADE;
                    END IF;
                END $migration$;

                INSERT INTO cliente_oficina (id_oficina, id_cliente, ativo, dados_permitidos, created_at, updated_at)
                SELECT DISTINCT
                    p.id_oficina,
                    p.id_cliente,
                    TRUE,
                    '["Nome","Email","Cpf_Cnpj","Telefones","Veiculos"]',
                    now(),
                    now()
                FROM pedido p
                INNER JOIN oficina o ON o.id = p.id_oficina
                INNER JOIN cliente c ON c.id = p.id_cliente
                ON CONFLICT (id_oficina, id_cliente) DO UPDATE
                SET ativo = TRUE,
                    updated_at = excluded.updated_at;

                DO $migration$
                BEGIN
                    IF to_regclass('public.oficina_cliente') IS NOT NULL THEN
                        EXECUTE '
                            INSERT INTO cliente_oficina (id_oficina, id_cliente, ativo, dados_permitidos, created_at, updated_at)
                            SELECT DISTINCT
                                oc.id_oficina,
                                oc.id_cliente,
                                TRUE,
                                ''["Nome","Email","Cpf_Cnpj","Telefones","Veiculos"]'',
                                now(),
                                now()
                            FROM oficina_cliente oc
                            INNER JOIN oficina o ON o.id = oc.id_oficina
                            INNER JOIN cliente c ON c.id = oc.id_cliente
                            ON CONFLICT (id_oficina, id_cliente) DO UPDATE
                            SET ativo = TRUE,
                                updated_at = excluded.updated_at';
                    END IF;
                END $migration$;

                DO $migration$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_pedido_oficina_cliente_id_oficina_id_cliente'
                    ) THEN
                        ALTER TABLE pedido
                            DROP CONSTRAINT "FK_pedido_oficina_cliente_id_oficina_id_cliente";
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_pedido_cliente_oficina_id_oficina_id_cliente'
                    ) THEN
                        ALTER TABLE pedido
                            ADD CONSTRAINT "FK_pedido_cliente_oficina_id_oficina_id_cliente"
                            FOREIGN KEY (id_oficina, id_cliente)
                            REFERENCES cliente_oficina (id_oficina, id_cliente)
                            ON DELETE RESTRICT;
                    END IF;
                END $migration$;

                CREATE INDEX IF NOT EXISTS "IX_cliente_oficina_id_cliente" ON cliente_oficina (id_cliente);
                CREATE INDEX IF NOT EXISTS "IX_cliente_oficina_oficina_ativo_cliente" ON cliente_oficina (id_oficina, ativo, id_cliente);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $migration$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_pedido_cliente_oficina_id_oficina_id_cliente'
                    ) THEN
                        ALTER TABLE pedido
                            DROP CONSTRAINT "FK_pedido_cliente_oficina_id_oficina_id_cliente";
                    END IF;

                    IF to_regclass('public.oficina_cliente') IS NOT NULL
                       AND NOT EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'FK_pedido_oficina_cliente_id_oficina_id_cliente'
                       ) THEN
                        ALTER TABLE pedido
                            ADD CONSTRAINT "FK_pedido_oficina_cliente_id_oficina_id_cliente"
                            FOREIGN KEY (id_oficina, id_cliente)
                            REFERENCES oficina_cliente (id_oficina, id_cliente)
                            ON DELETE RESTRICT;
                    END IF;
                END $migration$;
                """);
        }
    }
}
