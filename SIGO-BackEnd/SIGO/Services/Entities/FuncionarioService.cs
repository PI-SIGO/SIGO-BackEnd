using AutoMapper;
using SIGO.Data.Interfaces;
using SIGO.Data.Repositories;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;
using SIGO.Services.Interfaces;
using System.Linq;
using SIGO.Objects.Contracts;
using SIGO.Security;
using SIGO.Validation;

namespace SIGO.Services.Entities
{
    public class FuncionarioService : GenericService<Funcionario, FuncionarioDTO>, IFuncionarioService
    {
        private readonly IFuncionarioRepository _funcionarioRepository;
        private readonly IMapper _mapper;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ICpfValidator _cpfValidator;

        public FuncionarioService(
            IFuncionarioRepository funcionarioRepository,
            IMapper mapper,
            ICpfValidator cpfValidator,
            IPasswordHasher passwordHasher)
            : base(funcionarioRepository, mapper)
        {
            _funcionarioRepository = funcionarioRepository;
            _mapper = mapper;
            _cpfValidator = cpfValidator;
            _passwordHasher = passwordHasher;
        }

        public async Task<FuncionarioDTO?> Login(Login login)
        {
            if (string.IsNullOrWhiteSpace(login?.Email) || string.IsNullOrEmpty(login.Password))
                return null;

            var funcionario = await _funcionarioRepository.GetByEmail(login.Email);

            if (funcionario is null || !_passwordHasher.Verify(login.Password, funcionario.Senha))
                return null;

            if (_passwordHasher.NeedsRehash(funcionario.Senha))
                await _funcionarioRepository.UpdatePasswordHash(funcionario.Id, _passwordHasher.Hash(login.Password));

            return _mapper.Map<FuncionarioDTO?>(funcionario);
        }

        public async Task<IEnumerable<FuncionarioDTO>> GetFuncionarioByNome(string nome)
        {
            var entities = await _funcionarioRepository.GetFuncionarioByNome(nome);
            return _mapper.Map<IEnumerable<FuncionarioDTO>>(entities);
        }

        public async Task<IEnumerable<FuncionarioDTO>> GetByOficina(int oficinaId)
        {
            var entities = await _funcionarioRepository.GetByOficina(oficinaId);
            return _mapper.Map<IEnumerable<FuncionarioDTO>>(entities);
        }

        public async Task<FuncionarioDTO?> GetByIdForOficina(int id, int oficinaId)
        {
            var entity = await _funcionarioRepository.GetByIdForOficina(id, oficinaId);
            return _mapper.Map<FuncionarioDTO?>(entity);
        }

        public async Task<IEnumerable<FuncionarioDTO>> GetFuncionarioByNomeForOficina(string nome, int oficinaId)
        {
            var entities = await _funcionarioRepository.GetFuncionarioByNomeForOficina(nome, oficinaId);
            return _mapper.Map<IEnumerable<FuncionarioDTO>>(entities);
        }

        public async Task<bool> ExistsInOficina(int funcionarioId, int oficinaId)
        {
            return await _funcionarioRepository.ExistsInOficina(funcionarioId, oficinaId);
        }

        public async Task Create(FuncionarioRequestDTO funcionarioDTO)
        {
            await ValidateFuncionario(funcionarioDTO);
            funcionarioDTO.Cpf = _cpfValidator.Normalize(funcionarioDTO.Cpf);
            funcionarioDTO.Role = NormalizeRole(funcionarioDTO.Role);
            funcionarioDTO.Senha = _passwordHasher.Hash(funcionarioDTO.Senha);

            var funcionario = _mapper.Map<Funcionario>(funcionarioDTO);
            await _funcionarioRepository.Add(funcionario);
        }

        public override async Task Update(FuncionarioDTO funcionarioDTO, int id)
        {
            await ValidateFuncionario(funcionarioDTO, id);
            funcionarioDTO.Cpf = _cpfValidator.Normalize(funcionarioDTO.Cpf);
            funcionarioDTO.Role = NormalizeRole(funcionarioDTO.Role);
            var existing = await GetExisting(id);
            ApplyUpdate(existing, funcionarioDTO);
            await _funcionarioRepository.SaveChanges();
        }

        public async Task Update(FuncionarioRequestDTO funcionarioDTO, int id)
        {
            await ValidateFuncionario(funcionarioDTO, id);
            funcionarioDTO.Cpf = _cpfValidator.Normalize(funcionarioDTO.Cpf);
            funcionarioDTO.Role = NormalizeRole(funcionarioDTO.Role);
            var existing = await GetExisting(id);
            ApplyUpdate(existing, funcionarioDTO);

            if (!string.IsNullOrWhiteSpace(funcionarioDTO.Senha))
                existing.Senha = _passwordHasher.Hash(funcionarioDTO.Senha);

            await _funcionarioRepository.SaveChanges();
        }

        public async Task ValidarCpf(string? cpf, int? ignoreId = null)
        {
            var errors = new List<ValidationError>();
            await AddCpfErrors(cpf, errors, ignoreId);
            ThrowIfInvalid(errors);
        }

        private async Task ValidateFuncionario(FuncionarioDTO funcionarioDTO, int? ignoreId = null)
        {
            var errors = new List<ValidationError>();
            await AddCpfErrors(funcionarioDTO.Cpf, errors, ignoreId);
            AddRoleErrors(funcionarioDTO.Role, errors);
            AddOficinaErrors(funcionarioDTO.IdOficina, funcionarioDTO.Role, errors);
            ThrowIfInvalid(errors);
        }

        private async Task AddCpfErrors(string? cpf, ICollection<ValidationError> errors, int? ignoreId = null)
        {
            if (!_cpfValidator.IsValid(cpf))
            {
                errors.Add(new ValidationError(nameof(FuncionarioDTO.Cpf), "CPF inválido."));
                return;
            }

            var cpfNormalizado = _cpfValidator.Normalize(cpf!);
            var existe = await _funcionarioRepository.ExistsByCpf(cpfNormalizado, ignoreId);

            if (existe)
                errors.Add(new ValidationError(nameof(FuncionarioDTO.Cpf), "CPF já cadastrado."));
        }

        private static void AddRoleErrors(string? role, ICollection<ValidationError> errors)
        {
            if (string.IsNullOrWhiteSpace(role))
                return;

            if (!string.Equals(role, SystemRoles.Admin, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(role, SystemRoles.Funcionario, StringComparison.OrdinalIgnoreCase))
                errors.Add(new ValidationError(nameof(FuncionarioDTO.Role), "Role inválida."));
        }

        private static void AddOficinaErrors(int? oficinaId, string? role, ICollection<ValidationError> errors)
        {
            if (NormalizeRole(role) == SystemRoles.Admin)
                return;

            if (!oficinaId.HasValue || oficinaId.Value <= 0)
                errors.Add(new ValidationError(nameof(FuncionarioDTO.IdOficina), "Funcionário deve estar vinculado a uma oficina."));
        }

        private static void ThrowIfInvalid(IReadOnlyCollection<ValidationError> errors)
        {
            if (errors.Count > 0)
                throw new BusinessValidationException(errors);
        }

        private async Task<Funcionario> GetExisting(int id)
        {
            var existing = await _funcionarioRepository.GetById(id);
            if (existing is null)
                throw new KeyNotFoundException($"Funcionário com id {id} não encontrado.");

            return existing;
        }

        private static void ApplyUpdate(Funcionario existing, FuncionarioDTO funcionarioDTO)
        {
            existing.Nome = funcionarioDTO.Nome;
            existing.Cpf = funcionarioDTO.Cpf;
            existing.Cargo = funcionarioDTO.Cargo;
            existing.Email = funcionarioDTO.Email;
            existing.Situacao = funcionarioDTO.Situacao;
            existing.IdOficina = funcionarioDTO.IdOficina;
            existing.Role = NormalizeRole(funcionarioDTO.Role);
        }

        private static string NormalizeRole(string? role)
        {
            return string.Equals(role, SystemRoles.Admin, StringComparison.OrdinalIgnoreCase)
                ? SystemRoles.Admin
                : SystemRoles.Funcionario;
        }

    }
}
