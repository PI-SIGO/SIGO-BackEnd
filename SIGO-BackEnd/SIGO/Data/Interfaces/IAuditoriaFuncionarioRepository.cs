using SIGO.Objects.Models;

namespace SIGO.Data.Interfaces
{
    public interface IAuditoriaFuncionarioRepository
    {
        Task Add(AuditoriaFuncionario auditoria);

        Task<IEnumerable<AuditoriaFuncionario>> Get(
            int? funcionarioId = null,
            string? acao = null,
            string? entidade = null,
            DateTime? inicio = null,
            DateTime? fim = null,
            int? oficinaId = null);
    }
}
