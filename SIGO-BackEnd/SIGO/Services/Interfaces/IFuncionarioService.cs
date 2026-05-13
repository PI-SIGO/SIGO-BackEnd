using Microsoft.EntityFrameworkCore;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;
using SIGO.Objects.Contracts;

namespace SIGO.Services.Interfaces
{
    public interface IFuncionarioService : IGenericService<Funcionario, FuncionarioDTO>
    {
        Task<IEnumerable<FuncionarioDTO>> GetFuncionarioByNome(string nome);
        Task<IEnumerable<FuncionarioDTO>> GetByOficina(int oficinaId);
        Task<FuncionarioDTO?> GetByIdForOficina(int id, int oficinaId);
        Task<IEnumerable<FuncionarioDTO>> GetFuncionarioByNomeForOficina(string nome, int oficinaId);
        Task<bool> ExistsInOficina(int funcionarioId, int oficinaId);
        Task ValidarCpf(string? cpf, int? ignoreId = null);
        Task Create(FuncionarioRequestDTO funcionarioDTO);
        Task Update(FuncionarioRequestDTO funcionarioDTO, int id);

        Task<FuncionarioDTO?> Login(Login login);

    }
}
