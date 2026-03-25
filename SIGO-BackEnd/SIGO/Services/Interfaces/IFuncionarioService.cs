using Microsoft.EntityFrameworkCore;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;

namespace SIGO.Services.Interfaces
{
    public interface IFuncionarioService : IGenericService<Funcionario, FuncionarioDTO>
    {
        Task<IEnumerable<FuncionarioDTO>> GetFuncionarioByNome(string nome);
        Task ValidarCpf(string? cpf, int? ignoreId = null);

    }
}
