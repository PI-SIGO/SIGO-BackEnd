using SIGO.Integracao.Response;

namespace SIGO.Integracao.Interfaces
{
    public interface IViaCepIntegracao
    {
        Task<ViaCepResponse?> ObterDadosViaCep(string cep);
    }
}
