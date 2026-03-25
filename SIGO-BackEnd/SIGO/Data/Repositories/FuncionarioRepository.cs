using Microsoft.EntityFrameworkCore;
using SIGO.Data.Interfaces;
using SIGO.Objects.Models;
using System.Linq;

namespace SIGO.Data.Repositories
{
    public class FuncionarioRepository : GenericRepository<Funcionario>, IFuncionarioRepository
    {
        private readonly AppDbContext _context;

        public FuncionarioRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Funcionario>> GetFuncionarioByNome(string nome)
        {
            return await _context.Funcionarios
            .Where(f => f.Nome.Contains(nome))
            .ToListAsync();
        }

        public async Task<bool> ExistsByCpf(string cpf, int? ignoreId = null)
        {
            var cpfNormalizado = SomenteDigitos(cpf);

            return await _context.Funcionarios
                .AnyAsync(f =>
                    f.Cpf != null &&
                    f.Cpf.Replace(".", "").Replace("-", "") == cpfNormalizado &&
                    (!ignoreId.HasValue || f.Id != ignoreId.Value));
        }

        private static string SomenteDigitos(string valor) =>
            new(valor.Where(char.IsDigit).ToArray());
    }
}
