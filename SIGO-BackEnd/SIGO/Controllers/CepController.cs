using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SIGO.Errors;
using SIGO.Integracao.Interfaces;
using SIGO.Security;

namespace SIGO.Controllers
{
    [Route("api/v1/ceps")]
    [ApiController]
    [AllowAnonymous]
    public class CepController : ControllerBase
    {
        private readonly IViaCepIntegracao _viaCepIntegracao;

        public CepController(IViaCepIntegracao viaCepIntegracao)
        {
            _viaCepIntegracao = viaCepIntegracao;
        }

        [HttpGet("{cep}")]
        [EnableRateLimiting(RateLimitPolicies.CepLookup)]
        public async Task<IActionResult> ListarDadosEndereco([FromRoute] string cep)
        {
            var responseData = await _viaCepIntegracao.ObterDadosViaCep(cep);
            if (responseData is null)
            {
                return this.ApiProblem(
                    StatusCodes.Status404NotFound,
                    "CEP não encontrado.");
            }

            return Ok(responseData);
        }
    }
}
