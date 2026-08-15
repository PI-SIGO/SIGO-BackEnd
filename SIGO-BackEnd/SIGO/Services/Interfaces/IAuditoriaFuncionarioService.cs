using SIGO.Objects.Dtos.Entities;

namespace SIGO.Services.Interfaces
{
    public interface IAuditoriaFuncionarioService
    {
        Task Registrar(
            string acao,
            string entidade,
            int? entidadeId,
            string? descricao = null);

        Task<IEnumerable<AuditoriaFuncionarioDTO>> Get(
            int? funcionarioId = null,
            string? acao = null,
            string? entidade = null,
            DateTime? inicio = null,
            DateTime? fim = null);
    }
}