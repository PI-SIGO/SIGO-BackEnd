using Moq;
using SIGO.Data.Interfaces;
using SIGO.Objects.Models;
using SIGO.Services.Entities;
using Xunit;

namespace SIGO.Tests.Services
{
    public class OpcoesCadastroServiceTests
    {
        [Fact]
        public async Task GetByOficinaAsync_DeveAgruparRemoverDuplicadosEOrdenar()
        {
            var repository = new Mock<IOpcaoCadastroRepository>();
            repository
                .Setup(item => item.GetByOficinaAsync(12, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    Option("modeloVeiculo", " Corolla "),
                    Option("modeloVeiculo", "corolla"),
                    Option("modeloVeiculo", "Civic"),
                    Option("combustivel", "Gasolina"),
                    Option("cor", "Preto"),
                    Option("cargo", "Mecanico"),
                    Option("tipoMarca", "Importada"),
                    Option("fornecedor", "Auto Pecas Sul")
                });
            var service = new OpcoesCadastroService(repository.Object);

            var result = await service.GetByOficinaAsync(12);

            Assert.Equal(new[] { "Civic", "Corolla" }, result.ModelosVeiculo);
            Assert.Equal(new[] { "Gasolina" }, result.Combustiveis);
            Assert.Equal(new[] { "Preto" }, result.Cores);
            Assert.Equal(new[] { "Mecanico" }, result.Cargos);
            Assert.Equal(new[] { "Importada" }, result.TiposMarca);
            Assert.Equal(new[] { "Auto Pecas Sul" }, result.Fornecedores);
        }

        private static OpcaoCadastro Option(string category, string value) => new()
        {
            IdOficina = 12,
            Categoria = category,
            Valor = value
        };
    }
}
