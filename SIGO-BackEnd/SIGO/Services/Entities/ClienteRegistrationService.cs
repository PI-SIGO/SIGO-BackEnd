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

namespace SIGO.Services.Entities
{
    public sealed class ClienteRegistrationService : IClienteRegistrationService
    {
        private readonly IClienteIdentityRepository _identityRepository;
        private readonly IClienteContaRepository _contaRepository;
        private readonly ICpfValidator _cpfValidator;
        private readonly IPasswordHasher _passwordHasher;
        private readonly TimeProvider _timeProvider;

        public ClienteRegistrationService(
            IClienteIdentityRepository identityRepository,
            IClienteContaRepository contaRepository,
            ICpfValidator cpfValidator,
            IPasswordHasher passwordHasher,
            TimeProvider timeProvider)
        {
            _identityRepository = identityRepository;
            _contaRepository = contaRepository;
            _cpfValidator = cpfValidator;
            _passwordHasher = passwordHasher;
            _timeProvider = timeProvider;
        }

        public async Task<CadastroClienteResultadoDTO> RegisterAsync(
            CadastrarClienteDTO request,
            SecurityAuditContext auditContext,
            CancellationToken cancellationToken = default)
        {
            EnsureRequestIsValid(request);

            var cpf = _cpfValidator.Normalize(request.Cpf);
            var email = NormalizeEmail(request.Email);
            var passwordHash = _passwordHasher.Hash(request.Senha);
            var now = UtcNow();

            try
            {
                return await _identityRepository.ExecuteInTransactionAsync(
                    async token =>
                    {
                        var cliente = await _identityRepository.GetClienteByCpfAsync(cpf, token);
                        if (cliente is not null)
                        {
                            throw new ConflictException(
                                "Não foi possível concluir o cadastro com este CPF. " +
                                "Entre em contato com o suporte para verificar a titularidade.");
                        }

                        if (await _contaRepository.EmailInUseByOtherClienteAsync(
                                email,
                                null,
                                token))
                        {
                            throw new ConflictException("O e-mail informado já está em uso.");
                        }

                        cliente = new Cliente
                        {
                            Nome = request.Nome.Trim(),
                            Email = email,
                            Senha = null,
                            Cpf_Cnpj = cpf,
                            Numero = 0,
                            Sexo = Sexo.Outro,
                            TipoCliente = TipoCliente.FISICO,
                            Situacao = Situacao.ATIVO
                        };
                        await _identityRepository.AddClienteAsync(cliente, token);
                        await _identityRepository.SaveChangesAsync(token);

                        await _contaRepository.AddAsync(new ClienteConta
                        {
                            ClienteId = cliente.Id,
                            EmailNormalizado = email,
                            PasswordHash = passwordHash,
                            Status = EstadoClienteConta.Active,
                            TokenVersion = 1,
                            CreatedAt = now,
                            UpdatedAt = now
                        }, token);

                        var contato = await _identityRepository.GetContatoAsync(
                            cliente.Id,
                            TipoContatoCliente.Email,
                            email,
                            token);
                        if (contato is null)
                        {
                            await _identityRepository.AddContatoAsync(new ClienteContato
                            {
                                ClienteId = cliente.Id,
                                Tipo = TipoContatoCliente.Email,
                                ValorNormalizado = email,
                                Origem = OrigemContatoCliente.Cliente,
                                VerificadoEm = null,
                                CreatedAt = now
                            }, token);
                        }
                        else
                        {
                            contato.Origem = OrigemContatoCliente.Cliente;
                            contato.VerificadoEm = null;
                        }

                        await _identityRepository.AddAuditoriaAsync(new AuditoriaSeguranca
                        {
                            ClienteId = cliente.Id,
                            TipoAtor = auditContext.TipoAtor,
                            AtorId = auditContext.AtorId,
                            Evento = TipoEventoAuditoria.CadastroDiretoConcluido,
                            Resultado = ResultadoAuditoria.Sucesso,
                            DocumentoMascarado = ClienteDataMasking.MaskDocument(cpf),
                            ContatoMascarado = ClienteDataMasking.MaskContact(email),
                            IpAddress = auditContext.IpAddress,
                            CorrelationId = auditContext.CorrelationId,
                            CreatedAt = now
                        }, token);

                        await _identityRepository.SaveChangesAsync(token);

                        return new CadastroClienteResultadoDTO(
                            cliente.Id,
                            cliente.Nome,
                            email);
                    },
                    cancellationToken);
            }
            catch (PostgresException exception) when (
                exception.SqlState is PostgresErrorCodes.UniqueViolation or
                    PostgresErrorCodes.SerializationFailure)
            {
                throw new ConflictException("O cadastro foi realizado simultaneamente por outra requisição. Tente entrar ou tente novamente.");
            }
            catch (DbUpdateException exception) when (
                exception.InnerException is PostgresException postgresException &&
                postgresException.SqlState is PostgresErrorCodes.UniqueViolation or
                    PostgresErrorCodes.SerializationFailure)
            {
                throw new ConflictException("CPF ou e-mail já cadastrado.");
            }
        }

        private void EnsureRequestIsValid(CadastrarClienteDTO request)
        {
            var errors = new List<ValidationError>();
            if (request is null || !_cpfValidator.IsValid(request.Cpf))
                errors.Add(new ValidationError(nameof(CadastrarClienteDTO.Cpf), "CPF inválido."));
            if (request is null || string.IsNullOrWhiteSpace(request.Nome) || request.Nome.Trim().Length > 100)
                errors.Add(new ValidationError(nameof(CadastrarClienteDTO.Nome), "Nome obrigatório com até 100 caracteres."));
            if (request is null ||
                string.IsNullOrWhiteSpace(request.Email) ||
                request.Email.Length > 254 ||
                !MailAddress.TryCreate(request.Email.Trim(), out var address) ||
                !string.Equals(address.Address, request.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(new ValidationError(nameof(CadastrarClienteDTO.Email), "E-mail inválido."));
            }

            var password = request?.Senha;
            if (string.IsNullOrWhiteSpace(password) ||
                password.Length is < 8 or > 128 ||
                !password.Any(char.IsLetter) ||
                !password.Any(char.IsDigit))
            {
                errors.Add(new ValidationError(
                    nameof(CadastrarClienteDTO.Senha),
                    "Senha deve ter entre 8 e 128 caracteres, uma letra e um número."));
            }

            if (errors.Count > 0)
                throw new BusinessValidationException(errors);
        }

        private static string NormalizeEmail(string email) =>
            new MailAddress(email.Trim()).Address.ToLowerInvariant();

        private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;
    }
}
