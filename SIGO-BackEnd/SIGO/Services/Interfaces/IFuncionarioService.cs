using Microsoft.EntityFrameworkCore;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;
using SIGO.Objects.Contracts;

namespace SIGO.Services.Interfaces
{
    public interface IFuncionarioService : IGenericService<Funcionario, FuncionarioDTO>
    {
        Task<IEnumerable<FuncionarioDTO>> GetFuncionarioByNome(string nome);
        Task ValidarCpf(string? cpf, int? ignoreId = null);

        Task<FuncionarioDTO?> Login(Login login);

    }
}
