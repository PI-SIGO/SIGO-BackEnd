using SIGO.Integracao.Interfaces;
using SIGO.Integracao.Prefit;
using SIGO.Integracao.Response;
using System.Text.RegularExpressions;

namespace SIGO.Integracao
{
    public class ViaCepIntegracao : IViaCepIntegracao
    {
        public readonly IViaCepIntegracaoRefit _viaCepIntegracaoRefit;
        public ViaCepIntegracao(IViaCepIntegracaoRefit viaCepIntegracaoRefit)
        {
            _viaCepIntegracaoRefit = viaCepIntegracaoRefit;
        }

        public async Task<ViaCepResponse?> ObterDadosViaCep(string cep)
        {
            if (string.IsNullOrWhiteSpace(cep))
            {
                return null;
            }

            var cepNormalizado = Regex.Replace(cep, "[^0-9]", string.Empty);
            if (cepNormalizado.Length != 8)
            {
                return null;
            }

            var responseData = await _viaCepIntegracaoRefit.ObterDadosViaCep(cepNormalizado);
            if (responseData is not null && responseData.IsSuccessStatusCode && responseData.Content is not null)
            {
                if (responseData.Content.Erro == true)
                {
                    return null;
                }

                return responseData.Content;
            }

            return null;
        }
    }
}
