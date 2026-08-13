using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Dtos.Mappings;
using SIGO.Objects.Models;
using System.Text.Json;
using Xunit;

namespace SIGO.Tests.Mappings;

public sealed class PedidoMappingProfileTests
{
    [Fact]
    public void PedidoParaDto_DeveExporNomesDosRelacionamentos()
    {
        var mapper = CreateMapper();
        var pedido = new Pedido
        {
            Id = 10,
            Cliente = new Cliente { Id = 1, Nome = "Carlos" },
            Funcionario = new Funcionario { Id = 2, Nome = "Gustavo" },
            Oficina = new Oficina { Id = 3, Nome = "Oficina Central" },
            Veiculo = new Veiculo { Id = 4, NomeVeiculo = "Onix" },
            Pedido_Pecas = new List<Pedido_Peca>
            {
                new()
                {
                    IdPedido = 10,
                    IdPeca = 5,
                    Peca = new Peca { Id = 5, Nome = "Parafuso" },
                    Quantidade = 4,
                    ValorUnitario = 15m
                }
            },
            Pedido_Servicos = new List<Pedido_Servico>
            {
                new()
                {
                    IdPedido = 10,
                    IdServico = 6,
                    Servico = new Servico { Id = 6, Nome = "Troca de óleo" },
                    QuantVezes = 1,
                    ValorUnitario = 100m
                }
            }
        };

        var dto = mapper.Map<PedidoDTO>(pedido);

        Assert.Equal("Carlos", dto.NomeCliente);
        Assert.Equal("Gustavo", dto.NomeFuncionario);
        Assert.Equal("Oficina Central", dto.NomeOficina);
        Assert.Equal("Onix", dto.NomeVeiculo);
        Assert.Equal("Parafuso", dto.Pedido_Pecas.Single().NomePeca);
        Assert.Equal("Troca de óleo", dto.Pedido_Servicos.Single().NomeServico);
    }

    [Fact]
    public void NomesRelacionados_NaoDevemSerAceitosNoPayloadDoCliente()
    {
        const string json = """
            {
              "nomeCliente": "Forjado",
              "nomeFuncionario": "Forjado",
              "nomeOficina": "Forjada",
              "nomeVeiculo": "Forjado",
              "pedido_Pecas": [{ "nomePeca": "Forjada" }],
              "pedido_Servicos": [{ "nomeServico": "Forjado" }]
            }
            """;

        var dto = JsonSerializer.Deserialize<PedidoDTO>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(dto);
        Assert.Null(dto.NomeCliente);
        Assert.Null(dto.NomeFuncionario);
        Assert.Null(dto.NomeOficina);
        Assert.Null(dto.NomeVeiculo);
        Assert.Null(dto.Pedido_Pecas.Single().NomePeca);
        Assert.Null(dto.Pedido_Servicos.Single().NomeServico);
    }

    private static IMapper CreateMapper()
    {
        var configuration = new MapperConfiguration(
            config => config.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance);

        return configuration.CreateMapper();
    }
}
