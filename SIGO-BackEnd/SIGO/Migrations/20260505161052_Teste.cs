using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SIGO.Migrations
{
    /// <inheritdoc />
    public partial class Teste : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cliente",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    senha = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    cpf_cnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    obs = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    razao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    datanasc = table.Column<DateOnly>(type: "date", nullable: true),
                    sexo = table.Column<int>(type: "integer", nullable: false),
                    numero = table.Column<int>(type: "integer", nullable: false),
                    rua = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    cidade = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    cep = table.Column<string>(type: "text", nullable: false),
                    bairro = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    estado = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    pais = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    complemento = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    tipocliente = table.Column<int>(type: "integer", nullable: false),
                    situacao = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cliente", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "oficina",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    cnpj = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    numero = table.Column<int>(type: "integer", nullable: false),
                    rua = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    cidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    cep = table.Column<int>(type: "integer", nullable: false),
                    bairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    estado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    pais = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    complemento = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    senha = table.Column<string>(type: "text", nullable: false),
                    situacao = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oficina", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "servico",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: false),
                    valor = table.Column<double>(type: "double precision", nullable: false),
                    garantia = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_servico", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "compartilhamento_cliente",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_cliente = table.Column<int>(type: "integer", nullable: false),
                    codigo_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    dados_permitidos = table.Column<string>(type: "text", nullable: false),
                    expira_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    usado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compartilhamento_cliente", x => x.id);
                    table.ForeignKey(
                        name: "FK_compartilhamento_cliente_cliente_id_cliente",
                        column: x => x.id_cliente,
                        principalTable: "cliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "veiculo",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    placa = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    chassi = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: false),
                    ano = table.Column<int>(type: "integer", nullable: false),
                    quilometragem = table.Column<int>(type: "integer", nullable: false),
                    combustivel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    seguro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    cor = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    id_cliente = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_veiculo", x => x.id);
                    table.UniqueConstraint("AK_veiculo_id_id_cliente", x => new { x.id, x.id_cliente });
                    table.ForeignKey(
                        name: "FK_veiculo_cliente_id_cliente",
                        column: x => x.id_cliente,
                        principalTable: "cliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "compartilhamento_cliente_tentativa",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_oficina = table.Column<int>(type: "integer", nullable: false),
                    codigo_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    sucesso = table.Column<bool>(type: "boolean", nullable: false),
                    motivo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    tentado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compartilhamento_cliente_tentativa", x => x.id);
                    table.ForeignKey(
                        name: "FK_compartilhamento_cliente_tentativa_oficina_id_oficina",
                        column: x => x.id_oficina,
                        principalTable: "oficina",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "funcionario",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    cpf = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    cargo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    senha = table.Column<string>(type: "text", nullable: false),
                    situacao = table.Column<int>(type: "integer", nullable: false),
                    id_oficina = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_funcionario", x => x.id);
                    table.ForeignKey(
                        name: "FK_funcionario_oficina_id_oficina",
                        column: x => x.id_oficina,
                        principalTable: "oficina",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cliente_oficina",
                columns: table => new
                {
                    id_oficina = table.Column<int>(type: "integer", nullable: false),
                    id_cliente = table.Column<int>(type: "integer", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    dados_permitidos = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cliente_oficina", x => new { x.id_oficina, x.id_cliente });
                    table.ForeignKey(
                        name: "FK_cliente_oficina_cliente_id_cliente",
                        column: x => x.id_cliente,
                        principalTable: "cliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cliente_oficina_oficina_id_oficina",
                        column: x => x.id_oficina,
                        principalTable: "oficina",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "marca",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    desc = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tipomarca = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VeiculoId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marca", x => x.id);
                    table.ForeignKey(
                        name: "FK_marca_veiculo_VeiculoId",
                        column: x => x.VeiculoId,
                        principalTable: "veiculo",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "registro_servico",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_veiculo = table.Column<int>(type: "integer", nullable: false),
                    id_servico = table.Column<int>(type: "integer", nullable: true),
                    data_servico = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: true),
                    quilometragem = table.Column<int>(type: "integer", nullable: false),
                    responsavel = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registro_servico", x => x.id);
                    table.ForeignKey(
                        name: "FK_registro_servico_servico_id_servico",
                        column: x => x.id_servico,
                        principalTable: "servico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_registro_servico_veiculo_id_veiculo",
                        column: x => x.id_veiculo,
                        principalTable: "veiculo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "funcionario_servico",
                columns: table => new
                {
                    idFuncionario = table.Column<int>(type: "integer", nullable: false),
                    idServico = table.Column<int>(type: "integer", nullable: false),
                    tempodec = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_funcionario_servico", x => new { x.idFuncionario, x.idServico });
                    table.ForeignKey(
                        name: "FK_funcionario_servico_funcionario_idFuncionario",
                        column: x => x.idFuncionario,
                        principalTable: "funcionario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_funcionario_servico_servico_idServico",
                        column: x => x.idServico,
                        principalTable: "servico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "telefone",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    numero = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    ddd = table.Column<int>(type: "integer", maxLength: 3, nullable: false),
                    clienteid = table.Column<int>(type: "integer", nullable: false),
                    FuncionarioId = table.Column<int>(type: "integer", nullable: true),
                    OficinaId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_telefone", x => x.id);
                    table.ForeignKey(
                        name: "FK_telefone_cliente_clienteid",
                        column: x => x.clienteid,
                        principalTable: "cliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_telefone_funcionario_FuncionarioId",
                        column: x => x.FuncionarioId,
                        principalTable: "funcionario",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_telefone_oficina_OficinaId",
                        column: x => x.OficinaId,
                        principalTable: "oficina",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "pedido",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_cliente = table.Column<int>(type: "integer", nullable: false),
                    id_funcionario = table.Column<int>(type: "integer", nullable: false),
                    id_oficina = table.Column<int>(type: "integer", nullable: false),
                    id_veiculo = table.Column<int>(type: "integer", nullable: false),
                    valorTotal = table.Column<float>(type: "real", nullable: false),
                    descontoReais = table.Column<float>(type: "real", nullable: false),
                    descontoPorcentagem = table.Column<float>(type: "real", nullable: false),
                    descontoTotalReais = table.Column<float>(type: "real", nullable: false),
                    descontoServicoPorcentagem = table.Column<float>(type: "real", nullable: false),
                    descontoServicoReais = table.Column<float>(type: "real", nullable: false),
                    descontoPecaPorcentagem = table.Column<float>(type: "real", nullable: false),
                    descontoPecaReais = table.Column<float>(type: "real", nullable: false),
                    observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    dataInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    dataFim = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pedido", x => x.id);
                    table.ForeignKey(
                        name: "FK_pedido_cliente_id_cliente",
                        column: x => x.id_cliente,
                        principalTable: "cliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pedido_funcionario_id_funcionario",
                        column: x => x.id_funcionario,
                        principalTable: "funcionario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pedido_cliente_oficina_id_oficina_id_cliente",
                        columns: x => new { x.id_oficina, x.id_cliente },
                        principalTable: "cliente_oficina",
                        principalColumns: new[] { "id_oficina", "id_cliente" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pedido_oficina_id_oficina",
                        column: x => x.id_oficina,
                        principalTable: "oficina",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pedido_veiculo_id_veiculo_id_cliente",
                        columns: x => new { x.id_veiculo, x.id_cliente },
                        principalTable: "veiculo",
                        principalColumns: new[] { "id", "id_cliente" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "peca",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tipo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    valor = table.Column<float>(type: "real", nullable: false),
                    quantidade = table.Column<int>(type: "integer", nullable: false),
                    garantia = table.Column<DateOnly>(type: "date", nullable: false),
                    unidade = table.Column<int>(type: "integer", nullable: false),
                    idmarca = table.Column<int>(type: "integer", nullable: false),
                    dataAquisicao = table.Column<DateOnly>(type: "date", nullable: false),
                    fornecedor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_peca", x => x.id);
                    table.ForeignKey(
                        name: "FK_peca_marca_idmarca",
                        column: x => x.idmarca,
                        principalTable: "marca",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "peca_substituida",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_registro_servico = table.Column<int>(type: "integer", nullable: false),
                    nome = table.Column<string>(type: "text", nullable: true),
                    quantidade = table.Column<int>(type: "integer", nullable: false),
                    observacao = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_peca_substituida", x => x.id);
                    table.ForeignKey(
                        name: "FK_peca_substituida_registro_servico_id_registro_servico",
                        column: x => x.id_registro_servico,
                        principalTable: "registro_servico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pedido_servico",
                columns: table => new
                {
                    idPedido = table.Column<int>(type: "integer", nullable: false),
                    idServico = table.Column<int>(type: "integer", nullable: false),
                    quantVezes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pedido_servico", x => new { x.idPedido, x.idServico });
                    table.ForeignKey(
                        name: "FK_pedido_servico_pedido_idPedido",
                        column: x => x.idPedido,
                        principalTable: "pedido",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_pedido_servico_servico_idServico",
                        column: x => x.idServico,
                        principalTable: "servico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pedido_peca",
                columns: table => new
                {
                    idpedido = table.Column<int>(type: "integer", nullable: false),
                    idpeca = table.Column<int>(type: "integer", nullable: false),
                    quantidade = table.Column<int>(type: "integer", nullable: false),
                    datainstalacao = table.Column<DateOnly>(type: "date", nullable: false),
                    estado = table.Column<string>(type: "text", nullable: true),
                    observacao = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pedido_peca", x => new { x.idpedido, x.idpeca });
                    table.ForeignKey(
                        name: "FK_pedido_peca_peca_idpeca",
                        column: x => x.idpeca,
                        principalTable: "peca",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_pedido_peca_pedido_idpedido",
                        column: x => x.idpedido,
                        principalTable: "pedido",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_compartilhamento_cliente_codigo_hash",
                table: "compartilhamento_cliente",
                column: "codigo_hash",
                unique: true,
                filter: "ativo AND usado_em IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_compartilhamento_cliente_id_cliente",
                table: "compartilhamento_cliente",
                column: "id_cliente");

            migrationBuilder.CreateIndex(
                name: "IX_compartilhamento_cliente_tentativa_id_oficina_ip_address_te~",
                table: "compartilhamento_cliente_tentativa",
                columns: new[] { "id_oficina", "ip_address", "tentado_em" });

            migrationBuilder.CreateIndex(
                name: "IX_funcionario_id_oficina",
                table: "funcionario",
                column: "id_oficina");

            migrationBuilder.CreateIndex(
                name: "IX_funcionario_servico_idServico",
                table: "funcionario_servico",
                column: "idServico");

            migrationBuilder.CreateIndex(
                name: "IX_marca_VeiculoId",
                table: "marca",
                column: "VeiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_cliente_oficina_id_cliente",
                table: "cliente_oficina",
                column: "id_cliente");

            migrationBuilder.CreateIndex(
                name: "IX_peca_idmarca",
                table: "peca",
                column: "idmarca");

            migrationBuilder.CreateIndex(
                name: "IX_peca_substituida_id_registro_servico",
                table: "peca_substituida",
                column: "id_registro_servico");

            migrationBuilder.CreateIndex(
                name: "IX_pedido_id_cliente",
                table: "pedido",
                column: "id_cliente");

            migrationBuilder.CreateIndex(
                name: "IX_pedido_id_funcionario",
                table: "pedido",
                column: "id_funcionario");

            migrationBuilder.CreateIndex(
                name: "IX_pedido_id_oficina_id_cliente",
                table: "pedido",
                columns: new[] { "id_oficina", "id_cliente" });

            migrationBuilder.CreateIndex(
                name: "IX_pedido_id_veiculo_id_cliente",
                table: "pedido",
                columns: new[] { "id_veiculo", "id_cliente" });

            migrationBuilder.CreateIndex(
                name: "IX_pedido_peca_idpeca",
                table: "pedido_peca",
                column: "idpeca");

            migrationBuilder.CreateIndex(
                name: "IX_pedido_servico_idServico",
                table: "pedido_servico",
                column: "idServico");

            migrationBuilder.CreateIndex(
                name: "IX_registro_servico_id_servico",
                table: "registro_servico",
                column: "id_servico");

            migrationBuilder.CreateIndex(
                name: "IX_registro_servico_id_veiculo",
                table: "registro_servico",
                column: "id_veiculo");

            migrationBuilder.CreateIndex(
                name: "IX_telefone_clienteid",
                table: "telefone",
                column: "clienteid");

            migrationBuilder.CreateIndex(
                name: "IX_telefone_FuncionarioId",
                table: "telefone",
                column: "FuncionarioId");

            migrationBuilder.CreateIndex(
                name: "IX_telefone_OficinaId",
                table: "telefone",
                column: "OficinaId");

            migrationBuilder.CreateIndex(
                name: "IX_veiculo_id_cliente",
                table: "veiculo",
                column: "id_cliente");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "compartilhamento_cliente");

            migrationBuilder.DropTable(
                name: "compartilhamento_cliente_tentativa");

            migrationBuilder.DropTable(
                name: "funcionario_servico");

            migrationBuilder.DropTable(
                name: "peca_substituida");

            migrationBuilder.DropTable(
                name: "pedido_peca");

            migrationBuilder.DropTable(
                name: "pedido_servico");

            migrationBuilder.DropTable(
                name: "telefone");

            migrationBuilder.DropTable(
                name: "registro_servico");

            migrationBuilder.DropTable(
                name: "peca");

            migrationBuilder.DropTable(
                name: "pedido");

            migrationBuilder.DropTable(
                name: "servico");

            migrationBuilder.DropTable(
                name: "marca");

            migrationBuilder.DropTable(
                name: "funcionario");

            migrationBuilder.DropTable(
                name: "cliente_oficina");

            migrationBuilder.DropTable(
                name: "veiculo");

            migrationBuilder.DropTable(
                name: "oficina");

            migrationBuilder.DropTable(
                name: "cliente");
        }
    }
}
