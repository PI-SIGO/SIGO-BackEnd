using AutoMapper;
using SIGO.Data;
using SIGO.Data.Interfaces;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;
using SIGO.Objects.Enums;
using SIGO.Services.Interfaces;
using SIGO.Validation;
using System.Linq;

namespace SIGO.Services.Entities
{
    public class ClienteService : GenericService<Cliente, ClienteDTO>, IClienteService
    {
        private readonly IClienteRepository _clienteRepository;
        private readonly AppDbContext? _context;
        private readonly IMapper _mapper;
        private readonly ICpfCnpjValidator _cpfCnpjValidator;

        private readonly ITelefoneRepository _telefoneRepository;

        public ClienteService(
            IClienteRepository clienteRepository,
            ITelefoneRepository telefoneRepository,
            IMapper mapper,
            ICpfCnpjValidator cpfCnpjValidator,
            AppDbContext? context)
            : base(clienteRepository, mapper)
        {
            _clienteRepository = clienteRepository;
            _context = context;
            _telefoneRepository = telefoneRepository;
            _mapper = mapper;
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
                .Select(entity => MapClienteOficina(
                    entity,
                    oficinaId,
                    requireActiveLink: false))
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

        public override async Task<ClienteDTO?> GetById(int id)
        {
            var entity = await _clienteRepository.GetById(id);
            return _mapper.Map<ClienteDTO?>(entity);
        }

        public override async Task Update(ClienteDTO clienteDTO, int id)
        {
            var existingCliente = await _clienteRepository.GetById(id);
            if (existingCliente == null)
            {
                throw new KeyNotFoundException($"Cliente com id {id} não encontrado.");
            }

            ApplyClientTypeRules(clienteDTO);
            await ValidateCliente(clienteDTO, id);
            EnsureIdentityFieldsAreUnchanged(existingCliente, clienteDTO);
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

            await UpdateProfile(existingCliente, clienteDTO, id);
        }

        public async Task<ClienteOficinaDTO> UpdateForOficina(
            ClienteRequestDTO clienteDTO,
            int clienteId,
            int oficinaId)
        {
            var existingCliente = await _clienteRepository.GetByIdWithDetailsForOficina(
                clienteId,
                oficinaId);
            if (existingCliente is null)
            {
                throw new KeyNotFoundException("Cliente não encontrado para esta oficina.");
            }

            await UpdateProfile(existingCliente, clienteDTO, clienteId);

            return MapClienteOficina(existingCliente, oficinaId)
                ?? throw new KeyNotFoundException("Cliente não encontrado para esta oficina.");
        }

        private async Task UpdateProfile(
            Cliente existingCliente,
            ClienteRequestDTO clienteDTO,
            int clienteId)
        {
            if (!string.IsNullOrWhiteSpace(clienteDTO.senha))
            {
                throw new BusinessValidationException(new[]
                {
                    new ValidationError(nameof(ClienteRequestDTO.senha), "Use o endpoint específico de alteração de senha.")
                });
            }

            var profileUpdate = MapProfileUpdate(clienteDTO, clienteId);
            ApplyClientTypeRules(profileUpdate);
            await ValidateCliente(profileUpdate, clienteId);
            EnsureIdentityFieldsAreUnchanged(existingCliente, profileUpdate);
            await EnsureTelefoneIdsBelongToCliente(profileUpdate.Telefones, clienteId);
            profileUpdate.Cpf_Cnpj = _cpfCnpjValidator.Normalize(profileUpdate.Cpf_Cnpj!);

            await UpdateClienteAndTelefones(existingCliente, profileUpdate, clienteId);
        }

        public async Task<bool> ExistsInOficina(int clienteId, int oficinaId)
        {
            return await _clienteRepository.ExistsInOficina(clienteId, oficinaId);
        }

        public Task<bool> DeactivateAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return _clienteRepository.DeactivateAsync(id, cancellationToken);
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
            int clienteId)
        {
            if (_context is null)
            {
                await UpdateClienteAndTelefonesCore(existingCliente, clienteDTO, clienteId);
                return;
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            await UpdateClienteAndTelefonesCore(existingCliente, clienteDTO, clienteId);
            await transaction.CommitAsync();
        }

        private async Task UpdateClienteAndTelefonesCore(
            Cliente existingCliente,
            ClienteDTO clienteDTO,
            int clienteId)
        {
            ApplyUpdate(existingCliente, clienteDTO);

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

        private async Task ValidateCliente(ClienteDTO clienteDTO, int? ignoreId = null)
        {
            var errors = new List<ValidationError>();

            await AddCpfCnpjErrors(clienteDTO.Cpf_Cnpj, errors, ignoreId);
            await AddNomeEmailErrors(clienteDTO.Nome, clienteDTO.Email, errors, ignoreId);
            AddCepErrors(clienteDTO.Cep, errors);

            ThrowIfInvalid(errors);
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

        private static string SomenteDigitos(string? valor) =>
            new((valor ?? string.Empty).Where(char.IsDigit).ToArray());

        private static void EnsureIdentityFieldsAreUnchanged(Cliente existing, ClienteDTO request)
        {
            var errors = new List<ValidationError>();
            if (!string.Equals(
                    SomenteDigitos(existing.Cpf_Cnpj),
                    SomenteDigitos(request.Cpf_Cnpj),
                    StringComparison.Ordinal))
            {
                errors.Add(new ValidationError(
                    nameof(ClienteDTO.Cpf_Cnpj),
                    "CPF/CNPJ não pode ser alterado por este endpoint."));
            }

            var existingEmail = (existing.Email ?? string.Empty).Trim().ToLowerInvariant();
            var requestedEmail = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
            if (!string.Equals(existingEmail, requestedEmail, StringComparison.Ordinal))
            {
                errors.Add(new ValidationError(
                    nameof(ClienteDTO.Email),
                    "A alteração de e-mail não está disponível nesta versão."));
            }

            ThrowIfInvalid(errors);
        }

        private static void ThrowIfInvalid(IReadOnlyCollection<ValidationError> errors)
        {
            if (errors.Count > 0)
                throw new BusinessValidationException(errors);
        }

        private ClienteOficinaDTO? MapClienteOficina(
            Cliente cliente,
            int oficinaId,
            bool requireActiveLink = true)
        {
            var relacionamento = cliente.ClienteOficinas
                .FirstOrDefault(co =>
                    co.OficinaId == oficinaId &&
                    co.ClienteId == cliente.Id &&
                    (!requireActiveLink || co.Ativo));

            if (relacionamento is null)
                return null;

            var vehicles = _mapper.Map<List<VeiculoDTO>>(cliente.Veiculos)
                ?? new List<VeiculoDTO>();
            foreach (var vehicle in vehicles)
                RestrictVehicleHistoriesToOffice(vehicle, oficinaId);

            return new ClienteOficinaDTO
            {
                Id = cliente.Id,
                Nome = cliente.Nome,
                Email = cliente.Email,
                Cpf_Cnpj = cliente.Cpf_Cnpj,
                Obs = cliente.Obs,
                razao = cliente.Razao,
                DataNasc = cliente.DataNasc,
                Numero = cliente.Numero,
                Rua = cliente.Rua,
                Cidade = cliente.Cidade,
                Cep = cliente.Cep,
                Bairro = cliente.Bairro,
                Estado = cliente.Estado,
                Pais = cliente.Pais,
                Complemento = cliente.Complemento,
                Sexo = (int)cliente.Sexo,
                TipoCliente = (int)cliente.TipoCliente,
                Situacao = (int)cliente.Situacao,
                VinculoAtivo = relacionamento.Ativo,
                Telefones = _mapper.Map<List<TelefoneDTO>>(cliente.Telefones),
                Veiculos = vehicles
            };
        }

        private static void RestrictVehicleHistoriesToOffice(VeiculoDTO vehicle, int oficinaId)
        {
            vehicle.Pedidos = (vehicle.Pedidos ?? Array.Empty<PedidoDTO>())
                .Where(order => order.idOficina == oficinaId)
                .ToList();
            vehicle.RegistroServicos = (vehicle.RegistroServicos ?? Array.Empty<RegistroServicoDTO>())
                .Where(record => record.OficinaId == oficinaId)
                .ToList();
        }

        private static void ApplyUpdate(Cliente existing, ClienteDTO clienteDTO)
        {
            existing.Nome = clienteDTO.Nome;
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
        }

        private void ApplyClientTypeRules(ClienteDTO clienteDTO)
        {
            var normalizedDocument = _cpfCnpjValidator.Normalize(clienteDTO.Cpf_Cnpj ?? string.Empty);
            if (normalizedDocument.Length == 14)
            {
                if (string.IsNullOrWhiteSpace(clienteDTO.razao))
                {
                    throw new BusinessValidationException(new[]
                    {
                        new ValidationError(nameof(ClienteDTO.razao), "Razão social obrigatória para pessoa jurídica.")
                    });
                }

                clienteDTO.TipoCliente = (int)TipoCliente.JURIDICO;
                clienteDTO.Obs = string.Empty;
                clienteDTO.razao = clienteDTO.razao.Trim();
                return;
            }

            clienteDTO.TipoCliente = (int)TipoCliente.FISICO;
            clienteDTO.razao = string.Empty;
            clienteDTO.Obs = clienteDTO.Obs?.Trim() ?? string.Empty;
        }

        private static ClienteDTO MapProfileUpdate(ClienteRequestDTO request, int clienteId)
        {
            return new ClienteDTO
            {
                Id = clienteId,
                Nome = request.Nome,
                Email = request.Email,
                Cpf_Cnpj = request.Cpf_Cnpj,
                Obs = request.Obs,
                razao = request.razao,
                DataNasc = request.DataNasc,
                Numero = request.Numero,
                Rua = request.Rua,
                Cidade = request.Cidade,
                Cep = request.Cep,
                Bairro = request.Bairro,
                Estado = request.Estado,
                Pais = request.Pais,
                Complemento = request.Complemento,
                Sexo = request.Sexo,
                TipoCliente = request.TipoCliente,
                Telefones = request.Telefones ?? new List<TelefoneDTO>()
            };
        }

    }
}
