using SIGO.Objects.Models;

namespace SIGO.Data.Interfaces
{
    public interface IFuncionarioRepository : IGenericRepository<Funcionario>
    {
        Task<IEnumerable<Funcionario>> GetFuncionarioByNome(string nome);
        Task<IEnumerable<Funcionario>> GetByOficina(int oficinaId);
        Task<Funcionario?> GetByIdForOficina(int id, int oficinaId);
        Task<IEnumerable<Funcionario>> GetFuncionarioByNomeForOficina(string nome, int oficinaId);
        Task<bool> ExistsByCpf(string cpf, int? ignoreId = null);
        Task<bool> ExistsInOficina(int funcionarioId, int oficinaId);
        Task<Funcionario?> GetByEmail(string email);
        Task UpdatePasswordHash(int id, string passwordHash);
    }
}
