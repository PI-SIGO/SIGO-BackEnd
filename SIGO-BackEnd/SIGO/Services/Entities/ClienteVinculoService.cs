using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SIGO.Data.Interfaces;
using SIGO.Exceptions;
using SIGO.Objects.Contracts;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Enums;
using SIGO.Objects.Models;
using SIGO.Security;
using SIGO.Services.Interfaces;
using SIGO.Validation;

namespace SIGO.Services.Entities;

public sealed class ClienteVinculoService : IClienteVinculoService
{
    private readonly IClienteIdentityRepository _identityRepository;
    private readonly IClienteOficinaRepository _vinculoRepository;
    private readonly ICpfValidator _cpfValidator;
    private readonly TimeProvider _timeProvider;

    public ClienteVinculoService(
        IClienteIdentityRepository identityRepository,
        IClienteOficinaRepository vinculoRepository,
        ICpfValidator cpfValidator,
        TimeProvider timeProvider)
    {
        _identityRepository = identityRepository;
        _vinculoRepository = vinculoRepository;
        _cpfValidator = cpfValidator;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<VinculoClienteOficinaResumoDTO>> GetByClientAsync(
        int clienteId,
        CancellationToken cancellationToken = default)
    {
        var relationships = await _vinculoRepository.GetByClienteAsync(
            clienteId,
            cancellationToken);

        return relationships
            .Select(relationship => new VinculoClienteOficinaResumoDTO(
                relationship.OficinaId,
                relationship.Oficina?.Nome ?? "Oficina",
                relationship.Ativo,
                relationship.CreatedAt,
                relationship.RevogadoEm))
            .ToArray();
    }

    public async Task<PreCadastroClienteResultadoDTO> PreRegisterAsync(
        PreCadastrarClienteDTO request,
        int oficinaId,
        SecurityAuditContext auditContext,
        CancellationToken cancellationToken = default)
    {
        EnsurePreRegistrationIsValid(request, oficinaId);

        var cpf = _cpfValidator.Normalize(request.Cpf);
        var email = NormalizeEmail(request.Email);
        var phones = NormalizePhones(request);
        var now = UtcNow();

        try
        {
            return await _identityRepository.ExecuteInTransactionAsync(
                async token =>
                {
                    var cliente = await _identityRepository.GetClienteByCpfAsync(cpf, token);
                    var clienteCriado = cliente is null;
                    if (cliente is null)
                    {
                        cliente = CreateCliente(request, cpf, email, phones);
                        await _identityRepository.AddClienteAsync(cliente, token);
                        await _identityRepository.SaveChangesAsync(token);
                    }
                    else if (cliente.Situacao != Situacao.ATIVO)
                    {
                        throw new ConflictException("O cadastro deste CPF está inativo.");
                    }

                    var existingRelationship = await _vinculoRepository.GetAsync(
                        oficinaId,
                        cliente.Id,
                        token);
                    if (existingRelationship?.RevogadoEm is not null)
                    {
                        throw new ConflictException(
                            "O cliente revogou este vinculo. A oficina nao pode reativa-lo automaticamente.");
                    }

                    if (!clienteCriado && cliente.Conta is null)
                        CompleteMissingProfile(cliente, request, email, phones);

                    if (email is not null)
                    {
                        await AddUnverifiedContactIfMissingAsync(
                            cliente.Id,
                            TipoContatoCliente.Email,
                            email,
                            now,
                            token);
                    }

                    foreach (var phone in phones)
                    {
                        await AddUnverifiedContactIfMissingAsync(
                            cliente.Id,
                            TipoContatoCliente.Telefone,
                            phone.ContactValue,
                            now,
                            token);
                    }

                    var relationship = await _vinculoRepository.AddOrActivateAsync(
                        oficinaId,
                        cliente.Id,
                        token);

                    var contact = email ?? phones.FirstOrDefault()?.ContactValue;
                    await _identityRepository.AddAuditoriaAsync(new AuditoriaSeguranca
                    {
                        ClienteId = cliente.Id,
                        TipoAtor = auditContext.TipoAtor,
                        AtorId = auditContext.AtorId,
                        Evento = TipoEventoAuditoria.VinculoCriado,
                        Resultado = ResultadoAuditoria.Sucesso,
                        DocumentoMascarado = ClienteDataMasking.MaskDocument(cpf),
                        ContatoMascarado = contact is null
                            ? null
                            : ClienteDataMasking.MaskContact(contact),
                        IpAddress = auditContext.IpAddress,
                        CorrelationId = auditContext.CorrelationId,
                        CreatedAt = now
                    }, token);
                    await _identityRepository.SaveChangesAsync(token);

                    return new PreCadastroClienteResultadoDTO(
                        cliente.Id,
                        cliente.Nome,
                        cpf,
                        relationship.Ativo);
                },
                cancellationToken);
        }
        catch (PostgresException exception) when (IsConcurrentWrite(exception))
        {
            throw new ConflictException(
                "O vinculo foi alterado simultaneamente. Consulte os vinculos e tente novamente.");
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException postgresException &&
            IsConcurrentWrite(postgresException))
        {
            throw new ConflictException(
                "O vinculo foi alterado simultaneamente. Consulte os vinculos e tente novamente.");
        }
    }

    public async Task RevokeAsync(
        int clienteId,
        int oficinaId,
        SecurityAuditContext auditContext,
        CancellationToken cancellationToken = default)
    {
        await _identityRepository.ExecuteInTransactionAsync(
            async token =>
            {
                var now = UtcNow();
                var deactivated = await _vinculoRepository.DeactivateAsync(
                    oficinaId,
                    clienteId,
                    now,
                    token);
                if (!deactivated)
                    throw new KeyNotFoundException("Vínculo entre cliente e oficina não encontrado.");

                await AddLinkAuditAsync(
                    clienteId,
                    auditContext,
                    TipoEventoAuditoria.VinculoRevogado,
                    now,
                    token);
                return true;
            },
            cancellationToken);
    }

    public async Task DeactivateForOficinaAsync(
        int clienteId,
        int oficinaId,
        SecurityAuditContext auditContext,
        CancellationToken cancellationToken = default)
    {
        if (clienteId <= 0 || oficinaId <= 0)
        {
            throw new BusinessValidationException(new[]
            {
                new ValidationError("vinculo", "Cliente e oficina devem ser validos.")
            });
        }

        await _identityRepository.ExecuteInTransactionAsync(
            async token =>
            {
                var now = UtcNow();
                var deactivated = await _vinculoRepository.DeactivateByOficinaAsync(
                    oficinaId,
                    clienteId,
                    now,
                    token);
                if (!deactivated)
                    throw new KeyNotFoundException("Vinculo entre cliente e oficina nao encontrado.");

                await AddLinkAuditAsync(
                    clienteId,
                    auditContext,
                    TipoEventoAuditoria.VinculoRevogado,
                    now,
                    token);
                return true;
            },
            cancellationToken);
    }

    private static Cliente CreateCliente(
        PreCadastrarClienteDTO request,
        string cpf,
        string? email,
        IReadOnlyCollection<NormalizedPhone> phones)
    {
        var cliente = new Cliente
        {
            Nome = request.Nome.Trim(),
            Cpf_Cnpj = cpf,
            Email = email!,
            Senha = null!,
            Obs = TrimOrNull(request.Obs)!,
            Razao = TrimOrNull(request.Razao)!,
            DataNasc = request.DataNasc,
            Sexo = request.Sexo ?? Sexo.Outro,
            TipoCliente = TipoCliente.FISICO,
            Situacao = Situacao.ATIVO
        };

        ApplyAddress(cliente, request);
        AddMissingPhones(cliente, phones);
        return cliente;
    }

    private static void CompleteMissingProfile(
        Cliente cliente,
        PreCadastrarClienteDTO request,
        string? email,
        IReadOnlyCollection<NormalizedPhone> phones)
    {
        if (string.IsNullOrWhiteSpace(cliente.Nome))
            cliente.Nome = request.Nome.Trim();
        if (string.IsNullOrWhiteSpace(cliente.Email) && email is not null)
            cliente.Email = email;
        if (string.IsNullOrWhiteSpace(cliente.Obs))
            cliente.Obs = TrimOrNull(request.Obs)!;
        if (string.IsNullOrWhiteSpace(cliente.Razao))
            cliente.Razao = TrimOrNull(request.Razao)!;
        if (!cliente.DataNasc.HasValue && request.DataNasc.HasValue)
            cliente.DataNasc = request.DataNasc;
        if (cliente.Sexo == Sexo.Outro && request.Sexo.HasValue)
            cliente.Sexo = request.Sexo.Value;

        if (IsAddressEmpty(cliente) && HasAddressData(request))
            ApplyAddress(cliente, request);

        AddMissingPhones(cliente, phones);
    }

    private static void ApplyAddress(Cliente cliente, PreCadastrarClienteDTO request)
    {
        cliente.Numero = request.Numero ?? 0;
        cliente.Rua = TrimOrNull(request.Rua)!;
        cliente.Cidade = TrimOrNull(request.Cidade)!;
        cliente.Cep = NormalizeDigitsOrNull(request.Cep)!;
        cliente.Bairro = TrimOrNull(request.Bairro)!;
        cliente.Estado = TrimOrNull(request.Estado)!;
        cliente.Pais = TrimOrNull(request.Pais)!;
        cliente.Complemento = TrimOrNull(request.Complemento)!;
    }

    private static void AddMissingPhones(
        Cliente cliente,
        IReadOnlyCollection<NormalizedPhone> phones)
    {
        cliente.Telefones ??= new List<Telefone>();
        foreach (var phone in phones)
        {
            if (cliente.Telefones.Any(existing =>
                    existing.DDD == phone.DDD &&
                    string.Equals(existing.Numero, phone.Number, StringComparison.Ordinal)))
            {
                continue;
            }

            cliente.Telefones.Add(new Telefone
            {
                DDD = phone.DDD,
                Numero = phone.Number
            });
        }
    }

    private static bool IsAddressEmpty(Cliente cliente) =>
        cliente.Numero == 0 &&
        string.IsNullOrWhiteSpace(cliente.Rua) &&
        string.IsNullOrWhiteSpace(cliente.Cidade) &&
        string.IsNullOrWhiteSpace(cliente.Cep) &&
        string.IsNullOrWhiteSpace(cliente.Bairro) &&
        string.IsNullOrWhiteSpace(cliente.Estado) &&
        string.IsNullOrWhiteSpace(cliente.Pais) &&
        string.IsNullOrWhiteSpace(cliente.Complemento);

    private static bool HasAddressData(PreCadastrarClienteDTO request) =>
        request.Numero.HasValue ||
        !string.IsNullOrWhiteSpace(request.Rua) ||
        !string.IsNullOrWhiteSpace(request.Cidade) ||
        !string.IsNullOrWhiteSpace(request.Cep) ||
        !string.IsNullOrWhiteSpace(request.Bairro) ||
        !string.IsNullOrWhiteSpace(request.Estado) ||
        !string.IsNullOrWhiteSpace(request.Pais) ||
        !string.IsNullOrWhiteSpace(request.Complemento);

    private async Task AddUnverifiedContactIfMissingAsync(
        int clienteId,
        TipoContatoCliente type,
        string value,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var existing = await _identityRepository.GetContatoAsync(
            clienteId,
            type,
            value,
            cancellationToken);
        if (existing is not null)
            return;

        await _identityRepository.AddContatoAsync(new ClienteContato
        {
            ClienteId = clienteId,
            Tipo = type,
            ValorNormalizado = value,
            Origem = OrigemContatoCliente.Oficina,
            VerificadoEm = null,
            CreatedAt = now
        }, cancellationToken);
    }

    private async Task AddLinkAuditAsync(
        int clienteId,
        SecurityAuditContext auditContext,
        TipoEventoAuditoria eventType,
        DateTime now,
        CancellationToken cancellationToken)
    {
        await _identityRepository.AddAuditoriaAsync(new AuditoriaSeguranca
        {
            ClienteId = clienteId,
            TipoAtor = auditContext.TipoAtor,
            AtorId = auditContext.AtorId,
            Evento = eventType,
            Resultado = ResultadoAuditoria.Sucesso,
            IpAddress = auditContext.IpAddress,
            CorrelationId = auditContext.CorrelationId,
            CreatedAt = now
        }, cancellationToken);
        await _identityRepository.SaveChangesAsync(cancellationToken);
    }

    private void EnsurePreRegistrationIsValid(PreCadastrarClienteDTO request, int oficinaId)
    {
        var errors = new List<ValidationError>();
        if (oficinaId <= 0)
            errors.Add(new ValidationError("oficinaId", "Oficina inválida."));

        if (request is null)
        {
            errors.Add(new ValidationError("request", "O corpo da requisição é obrigatório."));
            throw new BusinessValidationException(errors);
        }

        if (!_cpfValidator.IsValid(request.Cpf))
            errors.Add(new ValidationError(nameof(PreCadastrarClienteDTO.Cpf), "CPF inválido."));
        if (string.IsNullOrWhiteSpace(request.Nome) || request.Nome.Trim().Length > 100)
            errors.Add(new ValidationError(nameof(PreCadastrarClienteDTO.Nome), "Nome obrigatório com até 100 caracteres."));
        if (!string.IsNullOrWhiteSpace(request.Email) && NormalizeEmail(request.Email) is null)
            errors.Add(new ValidationError(nameof(PreCadastrarClienteDTO.Email), "E-mail inválido."));

        if (!string.IsNullOrWhiteSpace(request.Telefone) &&
            NormalizeLegacyPhoneDigits(request.Telefone) is null)
        {
            errors.Add(new ValidationError(
                nameof(PreCadastrarClienteDTO.Telefone),
                "Telefone deve conter DDD e 8 ou 9 dígitos."));
        }

        AddMaxLengthError(request.Obs, 500, nameof(PreCadastrarClienteDTO.Obs), errors);
        AddMaxLengthError(request.Razao, 500, nameof(PreCadastrarClienteDTO.Razao), errors);
        AddMaxLengthError(request.Rua, 500, nameof(PreCadastrarClienteDTO.Rua), errors);
        AddMaxLengthError(request.Cidade, 500, nameof(PreCadastrarClienteDTO.Cidade), errors);
        AddMaxLengthError(request.Bairro, 500, nameof(PreCadastrarClienteDTO.Bairro), errors);
        AddMaxLengthError(request.Estado, 500, nameof(PreCadastrarClienteDTO.Estado), errors);
        AddMaxLengthError(request.Pais, 500, nameof(PreCadastrarClienteDTO.Pais), errors);
        AddMaxLengthError(request.Complemento, 500, nameof(PreCadastrarClienteDTO.Complemento), errors);

        if (!string.IsNullOrWhiteSpace(request.Cep) &&
            NormalizeDigitsOrNull(request.Cep)?.Length != 8)
        {
            errors.Add(new ValidationError(nameof(PreCadastrarClienteDTO.Cep), "CEP deve conter 8 dígitos."));
        }

        if (request.DataNasc.HasValue && request.DataNasc.Value > DateOnly.FromDateTime(DateTime.Today))
        {
            errors.Add(new ValidationError(
                nameof(PreCadastrarClienteDTO.DataNasc),
                "Data de nascimento não pode estar no futuro."));
        }

        if (request.Sexo.HasValue && !Enum.IsDefined(request.Sexo.Value))
            errors.Add(new ValidationError(nameof(PreCadastrarClienteDTO.Sexo), "Sexo inválido."));
        if (request.Numero < 0)
            errors.Add(new ValidationError(nameof(PreCadastrarClienteDTO.Numero), "Número do endereço não pode ser negativo."));

        var structuredPhones = request.Telefones ?? Array.Empty<PreCadastrarTelefoneClienteDTO>();
        var totalPhones = structuredPhones.Count +
            (string.IsNullOrWhiteSpace(request.Telefone) ? 0 : 1);
        if (totalPhones > 5)
            errors.Add(new ValidationError(nameof(PreCadastrarClienteDTO.Telefones), "Informe no máximo 5 telefones."));

        foreach (var structuredPhone in structuredPhones)
        {
            if (!IsValidStructuredPhone(structuredPhone))
            {
                errors.Add(new ValidationError(
                    nameof(PreCadastrarClienteDTO.Telefones),
                    "Cada telefone deve possuir DDD válido e número com 8 ou 9 dígitos."));
                break;
            }
        }

        var normalizedStructuredPhones = structuredPhones
            .Select(phone => $"{phone.DDD}:{NormalizeDigitsOrNull(phone.Numero)}")
            .ToArray();
        if (normalizedStructuredPhones.Distinct(StringComparer.Ordinal).Count() != normalizedStructuredPhones.Length)
        {
            errors.Add(new ValidationError(
                nameof(PreCadastrarClienteDTO.Telefones),
                "Não informe telefones duplicados."));
        }

        if (errors.Count > 0)
            throw new BusinessValidationException(errors);
    }

    private static void AddMaxLengthError(
        string? value,
        int maxLength,
        string field,
        ICollection<ValidationError> errors)
    {
        if (value?.Trim().Length > maxLength)
            errors.Add(new ValidationError(field, $"Campo deve ter no máximo {maxLength} caracteres."));
    }

    private static string? NormalizeEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 254)
            return null;

        return MailAddress.TryCreate(value.Trim(), out var address) &&
               string.Equals(address.Address, value.Trim(), StringComparison.OrdinalIgnoreCase)
            ? address.Address.ToLowerInvariant()
            : null;
    }

    private static IReadOnlyCollection<NormalizedPhone> NormalizePhones(PreCadastrarClienteDTO request)
    {
        var phones = new List<NormalizedPhone>();
        var legacyDigits = NormalizeLegacyPhoneDigits(request.Telefone);
        if (legacyDigits is not null)
        {
            phones.Add(new NormalizedPhone(
                int.Parse(legacyDigits[..2]),
                legacyDigits[2..]));
        }

        foreach (var phone in request.Telefones ?? Array.Empty<PreCadastrarTelefoneClienteDTO>())
        {
            phones.Add(new NormalizedPhone(
                phone.DDD,
                NormalizeDigitsOrNull(phone.Numero)!));
        }

        return phones
            .DistinctBy(phone => phone.ContactValue, StringComparer.Ordinal)
            .ToArray();
    }

    private static string? NormalizeLegacyPhoneDigits(string? value)
    {
        var digits = NormalizeDigitsOrNull(value);
        if (digits is null)
            return null;

        if (digits.Length is 12 or 13 && digits.StartsWith("55", StringComparison.Ordinal))
            digits = digits[2..];

        return digits.Length is 10 or 11 ? digits : null;
    }

    private static bool IsValidStructuredPhone(PreCadastrarTelefoneClienteDTO? phone)
    {
        if (phone is null || phone.DDD is < 11 or > 99)
            return false;

        return NormalizeDigitsOrNull(phone.Numero)?.Length is 8 or 9;
    }

    private static string? NormalizeDigitsOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return new string(value.Where(char.IsDigit).ToArray());
    }

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsConcurrentWrite(PostgresException exception) =>
        exception.SqlState is PostgresErrorCodes.UniqueViolation or
            PostgresErrorCodes.SerializationFailure;

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

    private sealed record NormalizedPhone(int DDD, string Number)
    {
        public string ContactValue => $"{DDD:D2}{Number}";
    }
}
