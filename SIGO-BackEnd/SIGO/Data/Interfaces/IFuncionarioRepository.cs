using SIGO.Data.Repositories;
using SIGO.Objects.Models;
using SIGO.Services.Interfaces;

namespace SIGO.Data.Interfaces
{
    public interface IFuncionarioRepository : IGenericRepository<Funcionario>
    {
        Task<IEnumerable<Funcionario>> GetFuncionarioByNome(string nome);
        Task<bool> ExistsByCpf(string cpf, int? ignoreId = null);

    }
}
