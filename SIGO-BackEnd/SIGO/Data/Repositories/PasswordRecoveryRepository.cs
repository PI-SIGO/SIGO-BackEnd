using System.Data;
using Microsoft.EntityFrameworkCore;
using SIGO.Data.Interfaces;
using SIGO.Objects.Contracts;
using SIGO.Objects.Enums;
using SIGO.Objects.Models;
using SIGO.Security;

namespace SIGO.Data.Repositories
{
    public sealed class PasswordRecoveryRepository : IPasswordRecoveryRepository
    {
        private readonly AppDbContext _context;

        public PasswordRecoveryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<PasswordRecoveryAccount>> FindActiveAccountsByEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default)
        {
            var accounts = new List<PasswordRecoveryAccount>(3);

            accounts.AddRange(await _context.ClienteContas
                .AsNoTracking()
                .Where(conta =>
                    conta.EmailNormalizado == normalizedEmail &&
                    conta.Status == EstadoClienteConta.Active &&
                    conta.Cliente.Situacao == Situacao.ATIVO)
                .Select(conta => new PasswordRecoveryAccount(
                    TipoContaRecuperacao.Cliente,
                    conta.ClienteId,
                    conta.Cliente.Nome,
                    conta.EmailNormalizado))
                .ToListAsync(cancellationToken));

            accounts.AddRange(await _context.Funcionarios
                .AsNoTracking()
                .Where(funcionario =>
                    funcionario.Email != null &&
                    funcionario.Email.Trim().ToLower() == normalizedEmail &&
                    funcionario.Situacao == Situacao.ATIVO &&
                    (funcionario.Role == SystemRoles.Admin ||
                     (funcionario.Oficina != null &&
                      funcionario.Oficina.Situacao == Situacao.ATIVO)))
                .Select(funcionario => new PasswordRecoveryAccount(
                    TipoContaRecuperacao.Funcionario,
                    funcionario.Id,
                    funcionario.Nome,
                    normalizedEmail))
                .ToListAsync(cancellationToken));

            accounts.AddRange(await _context.Oficinas
                .AsNoTracking()
                .Where(oficina =>
                    oficina.Email != null &&
                    oficina.Email.Trim().ToLower() == normalizedEmail &&
                    oficina.Situacao == Situacao.ATIVO)
                .Select(oficina => new PasswordRecoveryAccount(
                    TipoContaRecuperacao.Oficina,
                    oficina.Id,
                    oficina.Nome,
                    normalizedEmail))
                .ToListAsync(cancellationToken));

            return accounts
                .OrderBy(account => account.AccountType)
                .ThenBy(account => account.AccountId)
                .ToArray();
        }

        public async Task CreateTokenAsync(
            TokenRedefinicaoSenha token,
            DateTime invalidatedAt,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            try
            {
                await _context.TokensRedefinicaoSenha
                    .Where(existing =>
                        existing.TipoConta == token.TipoConta &&
                        existing.ContaId == token.ContaId &&
                        existing.UsadoEm == null)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            existing => existing.UsadoEm,
                            invalidatedAt),
                        cancellationToken);

                await _context.TokensRedefinicaoSenha.AddAsync(token, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<bool> IsTokenValidAsync(
            string tokenHash,
            DateTime utcNow,
            CancellationToken cancellationToken = default)
        {
            var token = await _context.TokensRedefinicaoSenha
                .AsNoTracking()
                .Where(item =>
                    item.TokenHash == tokenHash &&
                    item.UsadoEm == null &&
                    item.ExpiraEm > utcNow)
                .Select(item => new
                {
                    item.TipoConta,
                    item.ContaId
                })
                .SingleOrDefaultAsync(cancellationToken);

            return token is not null &&
                   await IsAccountActiveAsync(
                       token.TipoConta,
                       token.ContaId,
                       cancellationToken);
        }

        public async Task<bool> ResetPasswordAsync(
            string tokenHash,
            string newPasswordHash,
            DateTime utcNow,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            try
            {
                var token = await _context.TokensRedefinicaoSenha
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        item =>
                            item.TokenHash == tokenHash &&
                            item.UsadoEm == null &&
                            item.ExpiraEm > utcNow,
                        cancellationToken);

                if (token is null)
                    return false;

                var consumed = await _context.TokensRedefinicaoSenha
                    .Where(item =>
                        item.Id == token.Id &&
                        item.UsadoEm == null &&
                        item.ExpiraEm > utcNow)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(item => item.UsadoEm, utcNow),
                        cancellationToken);

                if (consumed != 1)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return false;
                }

                var passwordUpdated = token.TipoConta switch
                {
                    TipoContaRecuperacao.Cliente => await UpdateClientePasswordAsync(
                        token.ContaId,
                        newPasswordHash,
                        utcNow,
                        cancellationToken),
                    TipoContaRecuperacao.Funcionario => await UpdateFuncionarioPasswordAsync(
                        token.ContaId,
                        newPasswordHash,
                        cancellationToken),
                    TipoContaRecuperacao.Oficina => await UpdateOficinaPasswordAsync(
                        token.ContaId,
                        newPasswordHash,
                        cancellationToken),
                    _ => 0
                };

                if (passwordUpdated != 1)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return false;
                }

                await transaction.CommitAsync(cancellationToken);
                return true;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private Task<bool> IsAccountActiveAsync(
            TipoContaRecuperacao accountType,
            int accountId,
            CancellationToken cancellationToken)
        {
            return accountType switch
            {
                TipoContaRecuperacao.Cliente => _context.ClienteContas.AnyAsync(
                    conta =>
                        conta.ClienteId == accountId &&
                        conta.Status == EstadoClienteConta.Active &&
                        conta.Cliente.Situacao == Situacao.ATIVO,
                    cancellationToken),
                TipoContaRecuperacao.Funcionario => _context.Funcionarios.AnyAsync(
                    funcionario =>
                        funcionario.Id == accountId &&
                        funcionario.Situacao == Situacao.ATIVO &&
                        (funcionario.Role == SystemRoles.Admin ||
                         (funcionario.Oficina != null &&
                          funcionario.Oficina.Situacao == Situacao.ATIVO)),
                    cancellationToken),
                TipoContaRecuperacao.Oficina => _context.Oficinas.AnyAsync(
                    oficina =>
                        oficina.Id == accountId &&
                        oficina.Situacao == Situacao.ATIVO,
                    cancellationToken),
                _ => Task.FromResult(false)
            };
        }

        private Task<int> UpdateClientePasswordAsync(
            int clienteId,
            string passwordHash,
            DateTime utcNow,
            CancellationToken cancellationToken)
        {
            return _context.ClienteContas
                .Where(conta =>
                    conta.ClienteId == clienteId &&
                    conta.Status == EstadoClienteConta.Active &&
                    conta.Cliente.Situacao == Situacao.ATIVO)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(conta => conta.PasswordHash, passwordHash)
                        .SetProperty(conta => conta.TokenVersion, conta => conta.TokenVersion + 1)
                        .SetProperty(conta => conta.UpdatedAt, utcNow),
                    cancellationToken);
        }

        private Task<int> UpdateFuncionarioPasswordAsync(
            int funcionarioId,
            string passwordHash,
            CancellationToken cancellationToken)
        {
            return _context.Funcionarios
                .Where(funcionario =>
                    funcionario.Id == funcionarioId &&
                    funcionario.Situacao == Situacao.ATIVO &&
                    (funcionario.Role == SystemRoles.Admin ||
                     (funcionario.Oficina != null &&
                      funcionario.Oficina.Situacao == Situacao.ATIVO)))
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        funcionario => funcionario.Senha,
                        passwordHash),
                    cancellationToken);
        }

        private Task<int> UpdateOficinaPasswordAsync(
            int oficinaId,
            string passwordHash,
            CancellationToken cancellationToken)
        {
            return _context.Oficinas
                .Where(oficina =>
                    oficina.Id == oficinaId &&
                    oficina.Situacao == Situacao.ATIVO)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        oficina => oficina.Senha,
                        passwordHash),
                    cancellationToken);
        }
    }
}
