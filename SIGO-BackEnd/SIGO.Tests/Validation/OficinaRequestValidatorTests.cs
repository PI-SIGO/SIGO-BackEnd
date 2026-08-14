using SIGO.Objects.Dtos.Entities;
using SIGO.Validation;
using Xunit;

namespace SIGO.Tests.Validation
{
    public sealed class OficinaRequestValidatorTests
    {
        private readonly OficinaRequestValidator _validator = new();

        [Fact]
        public void Validate_DeveRejeitarCepZero()
        {
            var result = _validator.Validate(new OficinaRequestDTO { Cep = 0 });

            Assert.False(result.IsValid);
            Assert.Contains(
                result.Errors,
                error => error.PropertyName == nameof(OficinaRequestDTO.Cep) &&
                         error.ErrorMessage == "CEP deve ser maior que zero.");
        }

        [Fact]
        public void Validate_DeveAceitarCepMaiorQueZero()
        {
            var result = _validator.Validate(new OficinaRequestDTO { Cep = 89010000 });

            Assert.True(result.IsValid);
        }
    }
}
