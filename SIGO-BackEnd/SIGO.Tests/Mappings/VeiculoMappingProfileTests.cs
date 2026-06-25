using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Dtos.Mappings;
using SIGO.Objects.Models;
using Xunit;

namespace SIGO.Tests.Mappings
{
    public class VeiculoMappingProfileTests
    {
        [Fact]
        public void VeiculoParaDto_DeveMapearDadosAtrelados()
        {
            var mapper = CreateMapper();
            var veiculo = new Veiculo
            {
                Id = 1,
                ClienteId = 5,
                Imagens = new List<VeiculoImagem>
                {
                    new() { Id = 10, VeiculoId = 1, NomeOriginal = "frente.png" }
                },
                Marcas = new List<Marca>
                {
                    new() { Id = 20, Nome = "Fiat", Desc = "Marca", TipoMarca = "Automovel" }
                },
                RegistroServicos = new List<RegistroServico>
                {
                    new()
                    {
                        Id = 30,
                        VeiculoId = 1,
                        Descricao = "Revisao",
                        Responsavel = "Mecanico",
                        PecasSubstituidas = new List<PecaSubstituida>
                        {
                            new() { Id = 40, RegistroServicoId = 30, Nome = "Filtro", Quantidade = 1 }
                        }
                    }
                },
                Pedidos = new List<Pedido>
                {
                    new()
                    {
                        Id = 50,
                        idCliente = 5,
                        idVeiculo = 1,
                        Pedido_Pecas = new List<Pedido_Peca>
                        {
                            new() { IdPedido = 50, IdPeca = 60, Quantidade = 1 }
                        },
                        Pedido_Servicos = new List<Pedido_Servico>
                        {
                            new() { IdPedido = 50, IdServico = 70, QuantVezes = 1 }
                        }
                    }
                }
            };

            var dto = mapper.Map<VeiculoDTO>(veiculo);

            Assert.Single(dto.Imagens);
            Assert.Single(dto.Marcas);
            Assert.Single(dto.RegistroServicos);
            Assert.Single(dto.RegistroServicos.Single().PecasSubstituidas);
            Assert.Single(dto.Pedidos);
            Assert.Single(dto.Pedidos.Single().Pedido_Pecas);
            Assert.Single(dto.Pedidos.Single().Pedido_Servicos);
        }

        private static IMapper CreateMapper()
        {
            var config = new MapperConfiguration(
                cfg => cfg.AddProfile<MappingProfile>(),
                NullLoggerFactory.Instance);

            return config.CreateMapper();
        }
    }
}
