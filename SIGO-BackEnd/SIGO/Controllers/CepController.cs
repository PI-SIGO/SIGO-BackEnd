using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SIGO.Errors;
using SIGO.Integracao.Interfaces;

namespace SIGO.Controllers
{
    [Route("api/v1/ceps")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(Policy = SIGO.Security.AuthorizationPolicies.SelfServiceAccess)]
    public class CepController : ControllerBase
    {
        private readonly IViaCepIntegracao _viaCepIntegracao;

        public CepController(IViaCepIntegracao viaCepIntegracao)
        {
            _viaCepIntegracao = viaCepIntegracao;
        }

        [HttpGet("{cep}")]
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
