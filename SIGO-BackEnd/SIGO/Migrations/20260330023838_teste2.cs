using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SIGO.Migrations
{
    /// <inheritdoc />
    public partial class teste2 : Migration
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
                name: "funcionario",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    cpf = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    cargo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    situacao = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_funcionario", x => x.id);
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
                    situacao = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oficina", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Peca",
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
                    dataAquisicao = table.Column<DateOnly>(type: "date", nullable: false),
                    fornecedor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Peca", x => x.id);
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
                    status = table.Column<int>(type: "integer", nullable: false),
                    id_cliente = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_veiculo", x => x.id);
                    table.ForeignKey(
                        name: "FK_veiculo_cliente_id_cliente",
                        column: x => x.id_cliente,
                        principalTable: "cliente",
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
                name: "Cores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NomeCor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VeiculoId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cores_veiculo_VeiculoId",
                        column: x => x.VeiculoId,
                        principalTable: "veiculo",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "marca",
                columns: table => new
                {
                    idMarca = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nomeMarca = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    descMarca = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tipoMarca = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VeiculoId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marca", x => x.idMarca);
                    table.ForeignKey(
                        name: "FK_marca_veiculo_VeiculoId",
                        column: x => x.VeiculoId,
                        principalTable: "veiculo",
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
                        name: "FK_pedido_oficina_id_oficina",
                        column: x => x.id_oficina,
                        principalTable: "oficina",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pedido_veiculo_id_veiculo",
                        column: x => x.id_veiculo,
                        principalTable: "veiculo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "peca_marca",
                columns: table => new
                {
                    id_peca = table.Column<int>(type: "integer", nullable: false),
                    id_marca = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_peca_marca", x => new { x.id_peca, x.id_marca });
                    table.ForeignKey(
                        name: "FK_peca_marca_Peca_id_peca",
                        column: x => x.id_peca,
                        principalTable: "Peca",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_peca_marca_marca_id_marca",
                        column: x => x.id_marca,
                        principalTable: "marca",
                        principalColumn: "idMarca",
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
                        name: "FK_pedido_peca_Peca_idpeca",
                        column: x => x.idpeca,
                        principalTable: "Peca",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_pedido_peca_pedido_idpedido",
                        column: x => x.idpedido,
                        principalTable: "pedido",
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

            migrationBuilder.CreateIndex(
                name: "IX_Cores_VeiculoId",
                table: "Cores",
                column: "VeiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_funcionario_servico_idServico",
                table: "funcionario_servico",
                column: "idServico");

            migrationBuilder.CreateIndex(
                name: "IX_marca_VeiculoId",
                table: "marca",
                column: "VeiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_peca_marca_id_marca",
                table: "peca_marca",
                column: "id_marca");

            migrationBuilder.CreateIndex(
                name: "IX_pedido_id_cliente",
                table: "pedido",
                column: "id_cliente");

            migrationBuilder.CreateIndex(
                name: "IX_pedido_id_funcionario",
                table: "pedido",
                column: "id_funcionario");

            migrationBuilder.CreateIndex(
                name: "IX_pedido_id_oficina",
                table: "pedido",
                column: "id_oficina");

            migrationBuilder.CreateIndex(
                name: "IX_pedido_id_veiculo",
                table: "pedido",
                column: "id_veiculo");

            migrationBuilder.CreateIndex(
                name: "IX_pedido_peca_idpeca",
                table: "pedido_peca",
                column: "idpeca");

            migrationBuilder.CreateIndex(
                name: "IX_pedido_servico_idServico",
                table: "pedido_servico",
                column: "idServico");

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
                name: "Cores");

            migrationBuilder.DropTable(
                name: "funcionario_servico");

            migrationBuilder.DropTable(
                name: "peca_marca");

            migrationBuilder.DropTable(
                name: "pedido_peca");

            migrationBuilder.DropTable(
                name: "pedido_servico");

            migrationBuilder.DropTable(
                name: "telefone");

            migrationBuilder.DropTable(
                name: "marca");

            migrationBuilder.DropTable(
                name: "Peca");

            migrationBuilder.DropTable(
                name: "pedido");

            migrationBuilder.DropTable(
                name: "servico");

            migrationBuilder.DropTable(
                name: "funcionario");

            migrationBuilder.DropTable(
                name: "oficina");

            migrationBuilder.DropTable(
                name: "veiculo");

            migrationBuilder.DropTable(
                name: "cliente");
        }
    }
}
