using SIGO.Data.Interfaces;
using Xunit;

namespace SIGO.Tests.Security
{
    public class CompartilhamentoClienteAtomicContractTests
    {
        [Fact]
        public void Repositorio_DeveExporContratoDeResgateAtomico()
        {
            var method = typeof(ICompartilhamentoClienteRepository).GetMethod(nameof(ICompartilhamentoClienteRepository.RedeemValidByCodeHashAsync));

            Assert.NotNull(method);
        }
    }
}
