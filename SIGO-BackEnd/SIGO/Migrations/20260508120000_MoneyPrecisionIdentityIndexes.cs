using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SIGO.Data;

#nullable disable

namespace SIGO.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260508120000_MoneyPrecisionIdentityIndexes")]
    public partial class MoneyPrecisionIdentityIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            EnsureClienteOficinaTable(migrationBuilder);

            migrationBuilder.DropForeignKey(
                name: "FK_peca_marca_idmarca",
                table: "peca");

            migrationBuilder.DropForeignKey(
                name: "FK_pedido_peca_peca_idpeca",
                table: "pedido_peca");

            migrationBuilder.DropForeignKey(
                name: "FK_pedido_servico_servico_idServico",
                table: "pedido_servico");

            migrationBuilder.DropForeignKey(
                name: "FK_registro_servico_veiculo_id_veiculo",
                table: "registro_servico");

            migrationBuilder.AlterColumn<decimal>(
                name: "valor",
                table: "servico",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<decimal>(
                name: "valor",
                table: "peca",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<decimal>(
                name: "valorTotal",
                table: "pedido",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<decimal>(
                name: "descontoTotalReais",
                table: "pedido",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<decimal>(
                name: "descontoServicoReais",
                table: "pedido",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<decimal>(
                name: "descontoReais",
                table: "pedido",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<decimal>(
                name: "descontoPecaReais",
                table: "pedido",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<decimal>(
                name: "descontoServicoPorcentagem",
                table: "pedido",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<decimal>(
                name: "descontoPorcentagem",
                table: "pedido",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<decimal>(
                name: "descontoPecaPorcentagem",
                table: "pedido",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.CreateIndex(
                name: "IX_servico_id_oficina_nome",
                table: "servico",
                columns: new[] { "id_oficina", "nome" });

            migrationBuilder.CreateIndex(
                name: "IX_registro_servico_veiculo_data",
                table: "registro_servico",
                columns: new[] { "id_veiculo", "data_servico" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_peca_id_oficina_nome",
                table: "peca",
                columns: new[] { "id_oficina", "nome" });

            migrationBuilder.CreateIndex(
                name: "IX_pedido_id_veiculo_dataInicio",
                table: "pedido",
                columns: new[] { "id_veiculo", "dataInicio" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_oficina_cnpj",
                table: "oficina",
                column: "cnpj",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_oficina_email",
                table: "oficina",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_funcionario_cpf",
                table: "funcionario",
                column: "cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_funcionario_email",
                table: "funcionario",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_funcionario_id_oficina_nome",
                table: "funcionario",
                columns: new[] { "id_oficina", "nome" });

            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_funcionario_email_normalized"" ON funcionario (lower(btrim(email)));");

            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_funcionario_cpf_normalized"" ON funcionario (regexp_replace(cpf, '[^0-9]', '', 'g'));");

            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_oficina_email_normalized"" ON oficina (lower(btrim(email)));");

            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_oficina_cnpj_normalized"" ON oficina (regexp_replace(cnpj, '[^0-9]', '', 'g'));");

            migrationBuilder.Sql(
                @"CREATE INDEX IF NOT EXISTS ""IX_cliente_oficina_oficina_ativo_cliente"" ON cliente_oficina (id_oficina, ativo, id_cliente);");

            migrationBuilder.AddForeignKey(
                name: "FK_peca_marca_idmarca",
                table: "peca",
                column: "idmarca",
                principalTable: "marca",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_pedido_peca_peca_idpeca",
                table: "pedido_peca",
                column: "idpeca",
                principalTable: "peca",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_pedido_servico_servico_idServico",
                table: "pedido_servico",
                column: "idServico",
                principalTable: "servico",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_registro_servico_veiculo_id_veiculo",
                table: "registro_servico",
                column: "id_veiculo",
                principalTable: "veiculo",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_peca_marca_idmarca",
                table: "peca");

            migrationBuilder.DropForeignKey(
                name: "FK_pedido_peca_peca_idpeca",
                table: "pedido_peca");

            migrationBuilder.DropForeignKey(
                name: "FK_pedido_servico_servico_idServico",
                table: "pedido_servico");

            migrationBuilder.DropForeignKey(
                name: "FK_registro_servico_veiculo_id_veiculo",
                table: "registro_servico");

            migrationBuilder.DropIndex(
                name: "IX_servico_id_oficina_nome",
                table: "servico");

            migrationBuilder.DropIndex(
                name: "IX_registro_servico_veiculo_data",
                table: "registro_servico");

            migrationBuilder.DropIndex(
                name: "IX_peca_id_oficina_nome",
                table: "peca");

            migrationBuilder.DropIndex(
                name: "IX_pedido_id_veiculo_dataInicio",
                table: "pedido");

            migrationBuilder.DropIndex(
                name: "IX_oficina_cnpj",
                table: "oficina");

            migrationBuilder.DropIndex(
                name: "IX_oficina_email",
                table: "oficina");

            migrationBuilder.DropIndex(
                name: "IX_funcionario_cpf",
                table: "funcionario");

            migrationBuilder.DropIndex(
                name: "IX_funcionario_email",
                table: "funcionario");

            migrationBuilder.DropIndex(
                name: "IX_funcionario_id_oficina_nome",
                table: "funcionario");

            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_cliente_oficina_oficina_ativo_cliente"";");

            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_funcionario_email_normalized"";");

            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_funcionario_cpf_normalized"";");

            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_oficina_email_normalized"";");

            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_oficina_cnpj_normalized"";");

            migrationBuilder.AlterColumn<double>(
                name: "valor",
                table: "servico",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<float>(
                name: "valor",
                table: "peca",
                type: "real",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<float>(
                name: "valorTotal",
                table: "pedido",
                type: "real",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<float>(
                name: "descontoTotalReais",
                table: "pedido",
                type: "real",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<float>(
                name: "descontoServicoReais",
                table: "pedido",
                type: "real",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<float>(
                name: "descontoReais",
                table: "pedido",
                type: "real",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<float>(
                name: "descontoPecaReais",
                table: "pedido",
                type: "real",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<float>(
                name: "descontoServicoPorcentagem",
                table: "pedido",
                type: "real",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<float>(
                name: "descontoPorcentagem",
                table: "pedido",
                type: "real",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<float>(
                name: "descontoPecaPorcentagem",
                table: "pedido",
                type: "real",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AddForeignKey(
                name: "FK_peca_marca_idmarca",
                table: "peca",
                column: "idmarca",
                principalTable: "marca",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_pedido_peca_peca_idpeca",
                table: "pedido_peca",
                column: "idpeca",
                principalTable: "peca",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_pedido_servico_servico_idServico",
                table: "pedido_servico",
                column: "idServico",
                principalTable: "servico",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_registro_servico_veiculo_id_veiculo",
                table: "registro_servico",
                column: "id_veiculo",
                principalTable: "veiculo",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        private static void EnsureClienteOficinaTable(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS cliente_oficina (
                    id_oficina integer NOT NULL,
                    id_cliente integer NOT NULL,
                    ativo boolean NOT NULL DEFAULT TRUE,
                    dados_permitidos text NOT NULL DEFAULT '["nome"]',
                    created_at timestamp with time zone NOT NULL DEFAULT now(),
                    updated_at timestamp with time zone NOT NULL DEFAULT now()
                );

                ALTER TABLE cliente_oficina
                    ADD COLUMN IF NOT EXISTS ativo boolean NOT NULL DEFAULT TRUE,
                    ADD COLUMN IF NOT EXISTS dados_permitidos text NOT NULL DEFAULT '["nome"]',
                    ADD COLUMN IF NOT EXISTS created_at timestamp with time zone NOT NULL DEFAULT now(),
                    ADD COLUMN IF NOT EXISTS updated_at timestamp with time zone NOT NULL DEFAULT now();

                DO $$
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
                END $$;

                CREATE INDEX IF NOT EXISTS "IX_cliente_oficina_id_cliente" ON cliente_oficina (id_cliente);
                """);
        }
    }
}
