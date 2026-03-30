using AutoMapper;
using SIGO.Data.Interfaces;
using SIGO.Data.Repositories;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;
using SIGO.Services.Interfaces;
using System.Linq;
using SIGO.Objects.Contracts;

namespace SIGO.Services.Entities
{
    public class FuncionarioService : GenericService<Funcionario, FuncionarioDTO>, IFuncionarioService
    {
        private readonly IFuncionarioRepository _funcionarioRepository;
        private readonly IMapper _mapper;

        public FuncionarioService(IFuncionarioRepository funcionarioRepository, IMapper mapper)
            : base(funcionarioRepository, mapper)
        {
            _funcionarioRepository = funcionarioRepository;
            _mapper = mapper;
        }

        public async Task<FuncionarioDTO?> Login(Login login)
        {
            var funcionario = await _funcionarioRepository.Login(login);

            if (funcionario is not null) funcionario.Senha = "";
            return _mapper.Map<FuncionarioDTO?>(funcionario);
        }

        public async Task<IEnumerable<FuncionarioDTO>> GetFuncionarioByNome(string nome)
        {
            var entities = await _funcionarioRepository.GetFuncionarioByNome(nome);
            return _mapper.Map<IEnumerable<FuncionarioDTO>>(entities);
        }

        public new async Task Create(FuncionarioDTO funcionarioDTO)
        {
            await ValidarCpf(funcionarioDTO.Cpf);
            funcionarioDTO.Cpf = SomenteDigitos(funcionarioDTO.Cpf);
            await base.Create(funcionarioDTO);
        }

        public override async Task Update(FuncionarioDTO funcionarioDTO, int id)
        {
            await ValidarCpf(funcionarioDTO.Cpf, id);
            funcionarioDTO.Cpf = SomenteDigitos(funcionarioDTO.Cpf);
            await base.Update(funcionarioDTO, id);
        }

        public async Task ValidarCpf(string? cpf, int? ignoreId = null)
        {
            if (!IsCpfValido(cpf))
                throw new ArgumentException("CPF inválido.");

            var cpfNormalizado = SomenteDigitos(cpf!);
            var existe = await _funcionarioRepository.ExistsByCpf(cpfNormalizado, ignoreId);

            if (existe)
                throw new ArgumentException("CPF já cadastrado.");
        }

        private static bool IsCpfValido(string? cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
                return false;

            cpf = SomenteDigitos(cpf);
            if (cpf.Length != 11 || TodosCaracteresIguais(cpf))
                return false;

            var soma = 0;
            for (var i = 0; i < 9; i++)
                soma += (cpf[i] - '0') * (10 - i);

            var resto = (soma * 10) % 11;
            if (resto == 10 || resto == 11)
                resto = 0;

            if (resto != (cpf[9] - '0'))
                return false;

            soma = 0;
            for (var i = 0; i < 10; i++)
                soma += (cpf[i] - '0') * (11 - i);

            resto = (soma * 10) % 11;
            if (resto == 10 || resto == 11)
                resto = 0;

            return resto == (cpf[10] - '0');
        }

        private static string SomenteDigitos(string valor) =>
            new(valor.Where(char.IsDigit).ToArray());

        private static bool TodosCaracteresIguais(string valor) =>
            valor.All(c => c == valor[0]);

    }
}
