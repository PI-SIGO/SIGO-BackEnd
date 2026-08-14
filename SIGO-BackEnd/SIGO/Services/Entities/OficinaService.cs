using AutoMapper;
using SIGO.Data.Interfaces;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;
using SIGO.Services.Interfaces;
using System.Net.Mail;
using System.Linq;
using SIGO.Objects.Contracts;
using SIGO.Security;
using SIGO.Validation;

namespace SIGO.Services.Entities
{
    public class OficinaService : GenericService<Oficina, OficinaDTO>, IOficinaService
    {
        private readonly IOficinaRepository _oficinaRepository;
        private readonly IMapper _mapper;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ICnpjValidator _cnpjValidator;

        public OficinaService(
            IOficinaRepository oficinaRepository,
            IMapper mapper,
            ICnpjValidator cnpjValidator,
            IPasswordHasher passwordHasher)
            : base(oficinaRepository, mapper)
        {
            _oficinaRepository = oficinaRepository;
            _mapper = mapper;
            _cnpjValidator = cnpjValidator;
            _passwordHasher = passwordHasher;
        }

        public async Task<OficinaDTO?> Login(Login login)
        {
            if (string.IsNullOrWhiteSpace(login?.Email) || string.IsNullOrEmpty(login.Password))
                return null;

            var email = EmailNormalizer.Normalize(login.Email);
            var oficina = await _oficinaRepository.GetByEmail(email);

            if (oficina is null)
                return null;

            var passwordIsValid = _passwordHasher.Verify(login.Password, oficina.Senha);
            if (!passwordIsValid || oficina.Situacao != SIGO.Objects.Enums.Situacao.ATIVO)
                return null;

            if (_passwordHasher.NeedsRehash(oficina.Senha))
                await _oficinaRepository.UpdatePasswordHash(oficina.Id, _passwordHasher.Hash(login.Password));

            oficina.Senha = "";
            return _mapper.Map<OficinaDTO?>(oficina);
        }

        public async Task<IEnumerable<OficinaDTO>> GetByName(string nomeOficina)
        {
            var oficinas = await _oficinaRepository.GetByName(nomeOficina);
            return _mapper.Map<IEnumerable<OficinaDTO>>(oficinas);
        }

        public override async Task<OficinaDTO?> GetById(int id)
        {
            var oficina = await _oficinaRepository.GetActiveByIdAsync(id);
            return _mapper.Map<OficinaDTO?>(oficina);
        }

        public async Task Create(OficinaRequestDTO oficinaDTO)
        {
            await ValidateOficina(oficinaDTO);
            oficinaDTO.CNPJ = _cnpjValidator.Normalize(oficinaDTO.CNPJ!);
            oficinaDTO.Email = EmailNormalizer.Normalize(oficinaDTO.Email);
            oficinaDTO.Senha = _passwordHasher.Hash(oficinaDTO.Senha);

            var oficina = _mapper.Map<Oficina>(oficinaDTO);
            await _oficinaRepository.Add(oficina);
            oficinaDTO.Id = oficina.Id;
        }

        public override async Task Update(OficinaDTO oficinaDTO, int id)
        {
            await ValidateOficina(oficinaDTO, id);
            oficinaDTO.CNPJ = _cnpjValidator.Normalize(oficinaDTO.CNPJ!);
            oficinaDTO.Email = EmailNormalizer.Normalize(oficinaDTO.Email);
            var existing = await GetExisting(id);
            ApplyAdminUpdate(existing, oficinaDTO);
            await _oficinaRepository.SaveChanges();
        }

        public async Task Update(OficinaRequestDTO oficinaDTO, int id)
        {
            await ValidateOficina(oficinaDTO, id);
            oficinaDTO.CNPJ = _cnpjValidator.Normalize(oficinaDTO.CNPJ!);
            oficinaDTO.Email = EmailNormalizer.Normalize(oficinaDTO.Email);
            var existing = await GetExisting(id);
            ApplyAdminUpdate(existing, oficinaDTO);

            if (!string.IsNullOrWhiteSpace(oficinaDTO.Senha))
                existing.Senha = _passwordHasher.Hash(oficinaDTO.Senha);

            await _oficinaRepository.SaveChanges();
        }

        public async Task UpdateSelfProfile(OficinaRequestDTO oficinaDTO, int id)
        {
            ValidateCep(oficinaDTO.Cep);
            var existing = await GetExisting(id);

            existing.Nome = oficinaDTO.Nome;
            existing.Numero = oficinaDTO.Numero;
            existing.Rua = oficinaDTO.Rua;
            existing.Cidade = oficinaDTO.Cidade;
            existing.Cep = oficinaDTO.Cep;
            existing.Bairro = oficinaDTO.Bairro;
            existing.Estado = oficinaDTO.Estado;
            existing.Pais = oficinaDTO.Pais;
            existing.Complemento = oficinaDTO.Complemento;

            await _oficinaRepository.SaveChanges();
        }

        public async Task DeactivateAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            if (!await _oficinaRepository.DeactivateAsync(id, cancellationToken))
                throw new KeyNotFoundException($"Oficina com id {id} não encontrada.");
        }

        public async Task ValidarCnpj(string? cnpj, int? ignoreId = null)
        {
            var errors = new List<ValidationError>();
            await AddCnpjErrors(cnpj, errors, ignoreId);
            ThrowIfInvalid(errors);
        }

        private async Task ValidateOficina(OficinaDTO oficinaDTO, int? ignoreId = null)
        {
            var errors = new List<ValidationError>();
            await AddCnpjErrors(oficinaDTO.CNPJ, errors, ignoreId);
            await AddEmailErrors(oficinaDTO.Email, errors, ignoreId);
            AddCepErrors(oficinaDTO.Cep, errors);
            ThrowIfInvalid(errors);
        }

        private static void ValidateCep(int cep)
        {
            var errors = new List<ValidationError>();
            AddCepErrors(cep, errors);
            ThrowIfInvalid(errors);
        }

        private static void AddCepErrors(int cep, ICollection<ValidationError> errors)
        {
            if (cep <= 0)
                errors.Add(new ValidationError(nameof(OficinaDTO.Cep), "CEP deve ser maior que zero."));
        }

        private async Task AddCnpjErrors(string? cnpj, ICollection<ValidationError> errors, int? ignoreId = null)
        {
            if (!_cnpjValidator.IsValid(cnpj))
            {
                errors.Add(new ValidationError(nameof(OficinaDTO.CNPJ), "CNPJ inválido."));
                return;
            }

            var cnpjNormalizado = _cnpjValidator.Normalize(cnpj!);
            var existe = await _oficinaRepository.ExistsByCnpj(cnpjNormalizado, ignoreId);
            if (existe)
                errors.Add(new ValidationError(nameof(OficinaDTO.CNPJ), "CNPJ já cadastrado."));
        }

        private async Task AddEmailErrors(
            string? email,
            ICollection<ValidationError> errors,
            int? ignoreId = null)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                errors.Add(new ValidationError(nameof(OficinaDTO.Email), "E-mail obrigatório."));
                return;
            }

            var emailNormalizado = EmailNormalizer.Normalize(email);
            if (emailNormalizado.Length > 100 ||
                !MailAddress.TryCreate(emailNormalizado, out var address) ||
                !string.Equals(address.Address, emailNormalizado, StringComparison.Ordinal))
            {
                errors.Add(new ValidationError(nameof(OficinaDTO.Email), "E-mail inválido."));
                return;
            }

            if (await _oficinaRepository.ExistsByEmail(emailNormalizado, ignoreId))
                errors.Add(new ValidationError(nameof(OficinaDTO.Email), "E-mail já cadastrado."));
        }

        private static void ThrowIfInvalid(IReadOnlyCollection<ValidationError> errors)
        {
            if (errors.Count > 0)
                throw new BusinessValidationException(errors);
        }

        private async Task<Oficina> GetExisting(int id)
        {
            var existing = await _oficinaRepository.GetActiveByIdAsync(id);
            if (existing is null)
                throw new KeyNotFoundException($"Oficina com id {id} não encontrada.");

            return existing;
        }

        private static void ApplyAdminUpdate(Oficina existing, OficinaDTO oficinaDTO)
        {
            existing.Nome = oficinaDTO.Nome;
            existing.CNPJ = oficinaDTO.CNPJ;
            existing.Email = oficinaDTO.Email;
            existing.Numero = oficinaDTO.Numero;
            existing.Rua = oficinaDTO.Rua;
            existing.Cidade = oficinaDTO.Cidade;
            existing.Cep = oficinaDTO.Cep;
            existing.Bairro = oficinaDTO.Bairro;
            existing.Estado = oficinaDTO.Estado;
            existing.Pais = oficinaDTO.Pais;
            existing.Complemento = oficinaDTO.Complemento;
            existing.Situacao = oficinaDTO.Situacao;
        }

    }
}
