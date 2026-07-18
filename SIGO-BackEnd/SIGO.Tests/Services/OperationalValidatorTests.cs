using SIGO.Objects.Dtos.Entities;
using SIGO.Validation;
using Xunit;

namespace SIGO.Tests.Services
{
    public class OperationalValidatorTests
    {
        [Fact]
        public void PedidoValidator_RejeitaItensDuplicadosEDatasInvertidas()
        {
            var request = new PedidoDTO
            {
                idCliente = 1,
                idFuncionario = 1,
                idVeiculo = 1,
                DataInicio = new DateOnly(2026, 7, 14),
                DataFim = new DateOnly(2026, 7, 13),
                Pedido_Pecas = new List<Pedido_PecaDTO>
                {
                    new() { IdPeca = 1, Quantidade = 1, Estado = "Nova" },
                    new() { IdPeca = 1, Quantidade = 1, Estado = "Nova" }
                }
            };

            var result = new PedidoValidator().Validate(request);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error => error.PropertyName == nameof(PedidoDTO.DataFim));
            Assert.Contains(result.Errors, error => error.PropertyName == nameof(PedidoDTO.Pedido_Pecas));
        }
    }
}
