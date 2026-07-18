using System.ComponentModel.DataAnnotations;
using SIGO.Objects.Contracts;
using Xunit;

namespace SIGO.Tests.Contracts;

public sealed class PaginationContractsTests
{
    [Fact]
    public void Create_DeveRetornarSomentePaginaSolicitadaComMetadados()
    {
        var pagination = new PaginationRequest { Page = 2, PageSize = 2 };

        var result = PagedResponse<int>.Create(new[] { 1, 2, 3, 4, 5 }, pagination);

        Assert.Equal(new[] { 3, 4 }, result.Items);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(5, result.TotalItems);
        Assert.Equal(3, result.TotalPages);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void PaginationRequest_DeveRejeitarLimitesInvalidos(int page, int pageSize)
    {
        var request = new PaginationRequest { Page = page, PageSize = pageSize };
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            validationResults,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.NotEmpty(validationResults);
    }

    [Fact]
    public void Create_DeveRetornarVazioParaPaginaMuitoAltaSemOverflow()
    {
        var pagination = new PaginationRequest { Page = int.MaxValue, PageSize = 100 };

        var result = PagedResponse<int>.Create(new[] { 1, 2, 3 }, pagination);

        Assert.Empty(result.Items);
        Assert.Equal(int.MaxValue, result.Page);
    }
}
