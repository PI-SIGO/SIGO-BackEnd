using SIGO.Objects.Dtos.Entities;
using SIGO.Validation;
using Xunit;

namespace SIGO.Tests.Validation;

public class CatalogValidatorsTests
{
    [Fact]
    public void VeiculoValidator_DeveRejeitarQuilometragemNegativa()
    {
        var dto = CriarVeiculoValido() with { Quilometragem = -1 };

        var result = new VeiculoValidator().Validate(dto);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(VeiculoRequestDTO.Quilometragem));
    }

    [Fact]
    public void MarcaValidator_DeveRejeitarNomeVazio()
    {
        var result = new MarcaValidator().Validate(new MarcaDTO
        {
            Nome = string.Empty,
            Desc = "Descrição",
            TipoMarca = "Automóvel"
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(MarcaDTO.Nome));
    }

    [Theory]
    [InlineData("9999-9999")]
    [InlineData("99999-9999")]
    public void TelefoneValidator_DeveAceitarNumeroBrasileiroComOuSemNonoDigito(string numero)
    {
        var result = new TelefoneValidator().Validate(new TelefoneDTO
        {
            DDD = 47,
            Numero = numero,
            ClienteId = 1
        });

        Assert.True(result.IsValid);
    }

    private static VeiculoRequestDTO CriarVeiculoValido() => new()
    {
        NomeVeiculo = "Onix",
        TipoVeiculo = "Hatch",
        PlacaVeiculo = "ABC1D23",
        ChassiVeiculo = "9BGKS48U0KG000001",
        AnoFab = 2022,
        Quilometragem = 10_000,
        Combustivel = "Flex",
        Seguro = "Sim",
        Cor = "Prata"
    };
}
