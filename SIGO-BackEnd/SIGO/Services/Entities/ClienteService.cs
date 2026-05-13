using AutoMapper;
using SIGO.Data;
using SIGO.Data.Interfaces;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;
using SIGO.Security;
using SIGO.Services.Interfaces;
using SIGO.Utils;
using SIGO.Validation;
using System.Linq;

namespace SIGO.Services.Entities
{
    public class ClienteService : GenericService<Cliente, ClienteDTO>, IClienteService
    {
        private readonly IClienteRepository _clienteRepository;
        private readonly IClienteOficinaRepository _clienteOficinaRepository;
        private readonly AppDbContext? _context;
        private readonly IMapper _mapper;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ICpfCnpjValidator _cpfCnpjValidator;

        private readonly ITelefoneRepository _telefoneRepository;

        public ClienteService(
            IClienteRepository clienteRepository,
            ITelefoneRepository telefoneRepository,
            IMapper mapper,
            ICpfCnpjValidator cpfCnpjValidator,
            IClienteOficinaRepository clienteOficinaRepository,
            AppDbContext? context,
            IPasswordHasher passwordHasher)
            : base(clienteRepository, mapper)
        {
            _clienteRepository = clienteRepository;
            _clienteOficinaRepository = clienteOficinaRepository;
            _context = context;
            _telefoneRepository = telefoneRepository;
            _mapper = mapper;
            _passwordHasher = passwordHasher;
            _cpfCnpjValidator = cpfCnpjValidator;
        }
        public override async Task<IEnumerable<ClienteDTO>> GetAll()
        {
            var entities = await _clienteRepository.Get();
            return _mapper.Map<IEnumerable<ClienteDTO>>(entities);
        }

        public async Task<ClienteDTO?> GetByIdWithDetails(int id)
        {
            var entity = await _clienteRepository.GetByIdWithDetails(id);
            return _mapper.Map<ClienteDTO?>(entity);
        }

        public async Task<IEnumerable<ClienteOficinaDTO>> GetByOficina(int oficinaId)
        {
            var entities = await _clienteRepository.GetByOficina(oficinaId);
            return entities
                .Select(entity => MapClienteOficina(entity, oficinaId))
                .Where(dto => dto is not null)
                .Cast<ClienteOficinaDTO>();
        }

        public async Task<ClienteOficinaDTO?> GetByIdWithDetailsForOficina(int id, int oficinaId)
        {
            var entity = await _clienteRepository.GetByIdWithDetailsForOficina(id, oficinaId);
            return entity is null ? null : MapClienteOficina(entity, oficinaId);
        }

        public async Task<IEnumerable<ClienteDTO>> GetByNameWithDetails(string nome)
        {
            var entities = await _clienteRepository.GetByNameWithDetails(nome);
            return _mapper.Map<IEnumerable<ClienteDTO>>(entities);
        }

        public async Task<IEnumerable<ClienteOficinaDTO>> GetByNameWithDetailsForOficina(string nome, int oficinaId)
        {
            var entities = await _clienteRepository.GetByNameWithDetailsForOficina(nome, oficinaId);
            return entities
                .Select(entity => MapClienteOficina(entity, oficinaId))
                .Where(dto => dto is not null)
                .Cast<ClienteOficinaDTO>();
        }

        public async Task<ClienteDTO?> GetById(int id)
        {
            var entity = await _clienteRepository.GetById(id);
            return _mapper.Map<ClienteDTO?>(entity);
        }

        public async Task Create(ClienteRequestDTO clienteDTO)
        {
            await ValidateCliente(clienteDTO, requirePassword: true, senha: clienteDTO.senha);
            clienteDTO.Cpf_Cnpj = _cpfCnpjValidator.Normalize(clienteDTO.Cpf_Cnpj!);
            clienteDTO.senha = _passwordHasher.Hash(clienteDTO.senha);

            var cliente = _mapper.Map<Cliente>(clienteDTO);
            await _clienteRepository.Add(cliente);
        }

        public async Task<ClienteDTO> CreateForOficina(ClienteRequestDTO clienteDTO, int oficinaId)
        {
            if (_context is null)
                return await CreateForOficinaCore(clienteDTO, oficinaId);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            var cliente = await CreateForOficinaCore(clienteDTO, oficinaId);
            await transaction.CommitAsync();

            return cliente;
        }

        public override async Task Update(ClienteDTO clienteDTO, int id)
        {
            var existingCliente = await _clienteRepository.GetById(id);
            if (existingCliente == null)
            {
                throw new KeyNotFoundException($"Cliente com id {id} não encontrado.");
            }

            await ValidateCliente(clienteDTO, id);
            await EnsureTelefoneIdsBelongToCliente(clienteDTO.Telefones, id);
            clienteDTO.Cpf_Cnpj = _cpfCnpjValidator.Normalize(clienteDTO.Cpf_Cnpj!);

            clienteDTO.Id = id;

            await UpdateClienteAndTelefones(existingCliente, clienteDTO, id);
        }

        public async Task Update(ClienteRequestDTO clienteDTO, int id)
        {
            var existingCliente = await _clienteRepository.GetById(id);
            if (existingCliente == null)
            {
                throw new KeyNotFoundException($"Cliente com id {id} não encontrado.");
            }

            await ValidateCliente(clienteDTO, id, senha: clienteDTO.senha);
            await EnsureTelefoneIdsBelongToCliente(clienteDTO.Telefones, id);
            clienteDTO.Cpf_Cnpj = _cpfCnpjValidator.Normalize(clienteDTO.Cpf_Cnpj!);
            clienteDTO.Id = id;

            await UpdateClienteAndTelefones(existingCliente, clienteDTO, id, clienteDTO.senha);
        }
        public async Task<ClienteDTO?> Login(Login login)
        {
            if (string.IsNullOrWhiteSpace(login?.Email) || string.IsNullOrEmpty(login.Password))
                return null;

            var professor = await _clienteRepository.GetByEmail(login.Email);

            if (professor is null || !_passwordHasher.Verify(login.Password, professor.Senha))
                return null;

            if (_passwordHasher.NeedsRehash(professor.Senha))
                await _clienteRepository.UpdatePasswordHash(professor.Id, _passwordHasher.Hash(login.Password));

            return _mapper.Map<ClienteDTO>(professor);
        }

        public async Task<bool> ExistsInOficina(int clienteId, int oficinaId)
        {
            return await _clienteRepository.ExistsInOficina(clienteId, oficinaId);
        }

        public async Task<bool> AllowsFieldInOficina(int clienteId, int oficinaId, string campo)
        {
            return await _clienteRepository.AllowsFieldInOficina(clienteId, oficinaId, campo);
        }

        public async Task ValidarCpfCnpj(string? documento, int? ignoreId = null)
        {
            var errors = new List<ValidationError>();

            if (!_cpfCnpjValidator.IsValid(documento))
            {
                errors.Add(new ValidationError(nameof(ClienteDTO.Cpf_Cnpj), "CPF/CNPJ inválido."));
                throw new BusinessValidationException(errors);
            }

            var documentoNormalizado = _cpfCnpjValidator.Normalize(documento!);
            var existe = await _clienteRepository.ExistsByCpfCnpj(documentoNormalizado, ignoreId);

            if (existe)
                errors.Add(new ValidationError(nameof(ClienteDTO.Cpf_Cnpj), "CPF/CNPJ já cadastrado."));

            ThrowIfInvalid(errors);
        }

        public async Task ValidarNomeEmail(string? nome, string? email, int? ignoreId = null)
        {
            var errors = new List<ValidationError>();

            if (!string.IsNullOrWhiteSpace(nome))
            {
                var nomeJaExiste = await _clienteRepository.ExistsByNome(nome, ignoreId);
                if (nomeJaExiste)
                    errors.Add(new ValidationError(nameof(ClienteDTO.Nome), "Já existe cliente cadastrado com este nome."));
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                var emailJaExiste = await _clienteRepository.ExistsByEmail(email, ignoreId);
                if (emailJaExiste)
                    errors.Add(new ValidationError(nameof(ClienteDTO.Email), "Já existe cliente cadastrado com este e-mail."));
            }

            ThrowIfInvalid(errors);
        }

        private async Task<ClienteDTO> CreateForOficinaCore(ClienteRequestDTO clienteDTO, int oficinaId)
        {
            var errors = new List<ValidationError>();

            AddDocumentoFormatoErrors(clienteDTO.Cpf_Cnpj, errors);
            AddEmailObrigatorioErrors(clienteDTO.Email, errors);
            ThrowIfInvalid(errors);

            var documentoNormalizado = _cpfCnpjValidator.Normalize(clienteDTO.Cpf_Cnpj!);
            var emailNormalizado = NormalizeEmail(clienteDTO.Email);
            var clientesEncontrados = await _clienteRepository.GetByCpfCnpjOrEmail(documentoNormalizado, emailNormalizado);
            var clientesDistintos = clientesEncontrados
                .GroupBy(c => c.Id)
                .Select(g => g.First())
                .ToList();

            if (clientesDistintos.Count > 1)
            {
                throw new BusinessValidationException(new[]
                {
                    new ValidationError(
                        nameof(ClienteDTO.Cpf_Cnpj),
                        "CPF/CNPJ e e-mail pertencem a clientes diferentes.")
                });
            }

            Cliente cliente;
            if (clientesDistintos.Count == 1)
            {
                cliente = clientesDistintos[0];
                EnsureSameIdentity(cliente, documentoNormalizado, emailNormalizado);
            }
            else
            {
                await ValidateCliente(clienteDTO, requirePassword: true, senha: clienteDTO.senha);
                clienteDTO.Cpf_Cnpj = documentoNormalizado;
                clienteDTO.senha = _passwordHasher.Hash(clienteDTO.senha);
                cliente = _mapper.Map<Cliente>(clienteDTO);
                cliente = await _clienteRepository.Add(cliente);
            }

            await _clienteOficinaRepository.AddOrUpdatePermissoesAsync(
                oficinaId,
                cliente.Id,
                ClienteCompartilhamentoCampos.Serializar(ClienteCompartilhamentoCampos.Todos));

            return _mapper.Map<ClienteDTO>(cliente);
        }

        private async Task EnsureTelefoneIdsBelongToCliente(IEnumerable<TelefoneDTO>? telefones, int clienteId)
        {
            var telefoneIds = telefones?
                .Where(t => t.Id > 0)
                .Select(t => t.Id)
                .Distinct()
                .ToArray() ?? Array.Empty<int>();

            if (telefoneIds.Length == 0)
                return;

            var invalidTelefoneIds = await _telefoneRepository.GetInvalidIdsForCliente(clienteId, telefoneIds);
            if (invalidTelefoneIds.Count == 0)
                return;

            ThrowInvalidTelefoneIds(invalidTelefoneIds);
        }

        private async Task UpdateClienteAndTelefones(
            Cliente existingCliente,
            ClienteDTO clienteDTO,
            int clienteId,
            string? senha = null)
        {
            if (_context is null)
            {
                await UpdateClienteAndTelefonesCore(existingCliente, clienteDTO, clienteId, senha);
                return;
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            await UpdateClienteAndTelefonesCore(existingCliente, clienteDTO, clienteId, senha);
            await transaction.CommitAsync();
        }

        private async Task UpdateClienteAndTelefonesCore(
            Cliente existingCliente,
            ClienteDTO clienteDTO,
            int clienteId,
            string? senha)
        {
            ApplyUpdate(existingCliente, clienteDTO);

            if (!string.IsNullOrWhiteSpace(senha))
                existingCliente.Senha = _passwordHasher.Hash(senha);

            await _clienteRepository.SaveChanges();
            await SyncTelefones(clienteDTO.Telefones, clienteId);
        }

        private static void ThrowInvalidTelefoneIds(IEnumerable<int> invalidTelefoneIds)
        {
            throw new BusinessValidationException(new[]
            {
                new ValidationError(
                    nameof(ClienteDTO.Telefones),
                    $"Telefone(s) inválido(s) para este cliente: {string.Join(", ", invalidTelefoneIds)}.")
            });
        }

        private async Task SyncTelefones(IEnumerable<TelefoneDTO>? telefones, int clienteId)
        {
            if (telefones == null)
                return;

            foreach (var telefoneDto in telefones)
            {
                telefoneDto.ClienteId = clienteId;
                var telefoneEntity = _mapper.Map<Telefone>(telefoneDto);

                if (telefoneEntity.Id > 0)
                {
                    var updated = await _telefoneRepository.UpdateForCliente(telefoneEntity, clienteId);
                    if (!updated)
                        ThrowInvalidTelefoneIds(new[] { telefoneEntity.Id });
                }
                else
                {
                    await _telefoneRepository.Add(telefoneEntity);
                }
            }
        }

        private async Task ValidateCliente(ClienteDTO clienteDTO, int? ignoreId = null, bool requirePassword = false, string? senha = null)
        {
            var errors = new List<ValidationError>();

            await AddCpfCnpjErrors(clienteDTO.Cpf_Cnpj, errors, ignoreId);
            await AddNomeEmailErrors(clienteDTO.Nome, clienteDTO.Email, errors, ignoreId);
            AddCepErrors(clienteDTO.Cep, errors);
            AddSenhaErrors(senha, errors, requirePassword);

            ThrowIfInvalid(errors);
        }

        private void AddDocumentoFormatoErrors(string? documento, ICollection<ValidationError> errors)
        {
            if (!_cpfCnpjValidator.IsValid(documento))
                errors.Add(new ValidationError(nameof(ClienteDTO.Cpf_Cnpj), "CPF/CNPJ inválido."));
        }

        private static void AddEmailObrigatorioErrors(string? email, ICollection<ValidationError> errors)
        {
            if (string.IsNullOrWhiteSpace(email))
                errors.Add(new ValidationError(nameof(ClienteDTO.Email), "E-mail obrigatório."));
        }

        private async Task AddCpfCnpjErrors(string? documento, ICollection<ValidationError> errors, int? ignoreId = null)
        {
            if (!_cpfCnpjValidator.IsValid(documento))
            {
                errors.Add(new ValidationError(nameof(ClienteDTO.Cpf_Cnpj), "CPF/CNPJ inválido."));
                return;
            }

            var documentoNormalizado = _cpfCnpjValidator.Normalize(documento!);
            var existe = await _clienteRepository.ExistsByCpfCnpj(documentoNormalizado, ignoreId);

            if (existe)
                errors.Add(new ValidationError(nameof(ClienteDTO.Cpf_Cnpj), "CPF/CNPJ já cadastrado."));
        }

        private async Task AddNomeEmailErrors(string? nome, string? email, ICollection<ValidationError> errors, int? ignoreId = null)
        {
            if (!string.IsNullOrWhiteSpace(nome))
            {
                var nomeJaExiste = await _clienteRepository.ExistsByNome(nome, ignoreId);
                if (nomeJaExiste)
                    errors.Add(new ValidationError(nameof(ClienteDTO.Nome), "Já existe cliente cadastrado com este nome."));
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                var emailJaExiste = await _clienteRepository.ExistsByEmail(email, ignoreId);
                if (emailJaExiste)
                    errors.Add(new ValidationError(nameof(ClienteDTO.Email), "Já existe cliente cadastrado com este e-mail."));
            }
        }

        private static void AddCepErrors(string? cep, ICollection<ValidationError> errors)
        {
            var normalizedCep = new string((cep ?? string.Empty).Where(char.IsDigit).ToArray());
            if (normalizedCep.Length != 8)
                errors.Add(new ValidationError(nameof(ClienteDTO.Cep), "CEP inválido. O CEP deve conter 8 dígitos."));
        }

        private static void AddSenhaErrors(string? senha, ICollection<ValidationError> errors, bool requirePassword)
        {
            if (requirePassword && string.IsNullOrWhiteSpace(senha))
                errors.Add(new ValidationError(nameof(ClienteRequestDTO.senha), "Senha obrigatória."));
        }

        private static void EnsureSameIdentity(Cliente cliente, string documentoNormalizado, string emailNormalizado)
        {
            if (SomenteDigitos(cliente.Cpf_Cnpj) == documentoNormalizado &&
                NormalizeEmail(cliente.Email) == emailNormalizado)
                return;

            throw new BusinessValidationException(new[]
            {
                new ValidationError(
                    nameof(ClienteDTO.Cpf_Cnpj),
                    "CPF/CNPJ ou e-mail já cadastrado para outro cliente.")
            });
        }

        private static string NormalizeEmail(string email) =>
            email.Trim().ToLowerInvariant();

        private static string SomenteDigitos(string? valor) =>
            new((valor ?? string.Empty).Where(char.IsDigit).ToArray());

        private static void ThrowIfInvalid(IReadOnlyCollection<ValidationError> errors)
        {
            if (errors.Count > 0)
                throw new BusinessValidationException(errors);
        }

        private ClienteOficinaDTO? MapClienteOficina(Cliente cliente, int oficinaId)
        {
            var relacionamento = cliente.ClienteOficinas
                .FirstOrDefault(co => co.OficinaId == oficinaId && co.ClienteId == cliente.Id && co.Ativo);

            if (relacionamento is null)
                return null;

            var campos = ClienteCompartilhamentoCampos.Deserializar(relacionamento.DadosPermitidos);
            var dto = new ClienteOficinaDTO
            {
                Id = cliente.Id
            };

            if (campos.Contains(ClienteCompartilhamentoCampos.Nome))
                dto.Nome = cliente.Nome;

            if (campos.Contains(ClienteCompartilhamentoCampos.Email))
                dto.Email = cliente.Email;

            if (campos.Contains(ClienteCompartilhamentoCampos.CpfCnpj))
                dto.Cpf_Cnpj = cliente.Cpf_Cnpj;

            if (campos.Contains(ClienteCompartilhamentoCampos.Telefones))
                dto.Telefones = _mapper.Map<List<TelefoneDTO>>(cliente.Telefones);

            if (campos.Contains(ClienteCompartilhamentoCampos.Veiculos))
                dto.Veiculos = _mapper.Map<List<VeiculoDTO>>(cliente.Veiculos);

            return dto;
        }

        private static void ApplyUpdate(Cliente existing, ClienteDTO clienteDTO)
        {
            existing.Nome = clienteDTO.Nome;
            existing.Email = clienteDTO.Email;
            existing.Cpf_Cnpj = clienteDTO.Cpf_Cnpj;
            existing.Obs = clienteDTO.Obs;
            existing.Razao = clienteDTO.razao;
            existing.DataNasc = clienteDTO.DataNasc;
            existing.Numero = clienteDTO.Numero;
            existing.Rua = clienteDTO.Rua;
            existing.Cidade = clienteDTO.Cidade;
            existing.Cep = clienteDTO.Cep;
            existing.Bairro = clienteDTO.Bairro;
            existing.Estado = clienteDTO.Estado;
            existing.Pais = clienteDTO.Pais;
            existing.Complemento = clienteDTO.Complemento;
            existing.Sexo = (SIGO.Objects.Enums.Sexo)clienteDTO.Sexo;
            existing.TipoCliente = (SIGO.Objects.Enums.TipoCliente)clienteDTO.TipoCliente;
            existing.Situacao = (SIGO.Objects.Enums.Situacao)clienteDTO.Situacao;
        }

    }
}
