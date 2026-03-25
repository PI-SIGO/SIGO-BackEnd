using Refit;
using SIGO.Integracao.Response;

namespace SIGO.Integracao.Prefit
{
    public interface IViaCepIntegracaoRefit
    {
        [Get("/ws/{cep}/json/")]
        Task<ApiResponse<ViaCepResponse>> ObterDadosViaCep(string cep);
    }
}
