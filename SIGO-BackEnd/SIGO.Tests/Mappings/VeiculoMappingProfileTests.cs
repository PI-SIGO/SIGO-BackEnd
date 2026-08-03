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
                Status = SIGO.Objects.Enums.Status.Pendente,
                Imagens = new List<VeiculoImagem>
                {
                    new() { Id = 10, VeiculoId = 1, NomeOriginal = "frente.png" }
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
                            new()
                            {
                                IdPedido = 50,
                                IdPeca = 60,
                                Quantidade = 1,
                                ValorUnitario = 25m
                            }
                        },
                        Pedido_Servicos = new List<Pedido_Servico>
                        {
                            new()
                            {
                                IdPedido = 50,
                                IdServico = 70,
                                QuantVezes = 1,
                                ValorUnitario = 80m
                            }
                        }
                    }
                }
            };

            var dto = mapper.Map<VeiculoDTO>(veiculo);

            Assert.Equal(SIGO.Objects.Enums.Status.Pendente, dto.Status);
            Assert.Single(dto.Imagens);
            Assert.Single(dto.RegistroServicos);
            Assert.Single(dto.RegistroServicos.Single().PecasSubstituidas);
            Assert.Single(dto.Pedidos);
            Assert.Single(dto.Pedidos.Single().Pedido_Pecas);
            Assert.Single(dto.Pedidos.Single().Pedido_Servicos);
            Assert.Equal(25m, dto.Pedidos.Single().Pedido_Pecas.Single().ValorUnitario);
            Assert.Equal(80m, dto.Pedidos.Single().Pedido_Servicos.Single().ValorUnitario);
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
