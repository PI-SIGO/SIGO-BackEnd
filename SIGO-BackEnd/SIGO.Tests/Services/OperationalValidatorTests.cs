using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Enums;
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

        [Fact]
        public void AtualizarStatusRequestValidator_ExigeStatusValido()
        {
            var missing = new AtualizarStatusRequestValidator().Validate(
                new AtualizarStatusRequestDTO());
            var invalid = new AtualizarStatusRequestValidator().Validate(
                new AtualizarStatusRequestDTO { Status = (Status)999 });

            Assert.False(missing.IsValid);
            Assert.False(invalid.IsValid);
            Assert.All(missing.Errors, error => Assert.Equal(
                nameof(AtualizarStatusRequestDTO.Status),
                error.PropertyName));
            Assert.All(invalid.Errors, error => Assert.Equal(
                nameof(AtualizarStatusRequestDTO.Status),
                error.PropertyName));
        }
    }
}
