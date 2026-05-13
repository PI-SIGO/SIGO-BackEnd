using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SIGO.Data.Interfaces;
using SIGO.Objects.Models;

namespace SIGO.Data.Repositories
{
    public class CompartilhamentoClienteRepository : ICompartilhamentoClienteRepository
    {
        private readonly AppDbContext _context;

        public CompartilhamentoClienteRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ICompartilhamentoClienteTransaction> BeginTransactionAsync()
        {
            return new EfCompartilhamentoClienteTransaction(await _context.Database.BeginTransactionAsync());
        }

        public async Task AddAsync(CompartilhamentoCliente compartilhamento)
        {
            await _context.CompartilhamentosCliente.AddAsync(compartilhamento);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsByCodeHashAsync(string codigoHash)
        {
            return await _context.CompartilhamentosCliente
                .AnyAsync(c => c.CodigoHash == codigoHash && c.Ativo && c.UsadoEm == null);
        }

        public async Task<CompartilhamentoCliente?> GetByCodeHashAsync(string codigoHash)
        {
            return await _context.CompartilhamentosCliente
                .Include(c => c.Cliente)
                    .ThenInclude(c => c.Telefones)
                .Include(c => c.Cliente)
                    .ThenInclude(c => c.Veiculos)
                .FirstOrDefaultAsync(c => c.CodigoHash == codigoHash);
        }

        public async Task<CompartilhamentoCliente?> GetValidByCodeHashAsync(string codigoHash, DateTime agoraUtc)
        {
            return await _context.CompartilhamentosCliente
                .Include(c => c.Cliente)
                    .ThenInclude(c => c.Telefones)
                .Include(c => c.Cliente)
                    .ThenInclude(c => c.Veiculos)
                .FirstOrDefaultAsync(c =>
                    c.CodigoHash == codigoHash &&
                    c.Ativo &&
                    c.UsadoEm == null &&
                    c.ExpiraEm > agoraUtc);
        }

        public async Task<CompartilhamentoCliente?> RedeemValidByCodeHashAsync(string codigoHash, DateTime agoraUtc)
        {
            var affectedRows = await _context.CompartilhamentosCliente
                .Where(c =>
                    c.CodigoHash == codigoHash &&
                    c.Ativo &&
                    c.UsadoEm == null &&
                    c.ExpiraEm > agoraUtc)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(c => c.Ativo, false)
                    .SetProperty(c => c.UsadoEm, agoraUtc));

            if (affectedRows != 1)
                return null;

            return await _context.CompartilhamentosCliente
                .Include(c => c.Cliente)
                    .ThenInclude(c => c.Telefones)
                .Include(c => c.Cliente)
                    .ThenInclude(c => c.Veiculos)
                .FirstOrDefaultAsync(c => c.CodigoHash == codigoHash);
        }

        public async Task AddTentativaAsync(CompartilhamentoClienteTentativa tentativa)
        {
            await _context.CompartilhamentosClienteTentativas.AddAsync(tentativa);
            await _context.SaveChangesAsync();
        }

        public async Task<int> CountFalhasRecentesAsync(int oficinaId, string? ipAddress, DateTime desdeUtc)
        {
            return await _context.CompartilhamentosClienteTentativas
                .CountAsync(t =>
                    t.OficinaId == oficinaId &&
                    !t.Sucesso &&
                    t.TentadoEm >= desdeUtc &&
                    (ipAddress == null || t.IpAddress == ipAddress));
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        private sealed class EfCompartilhamentoClienteTransaction : ICompartilhamentoClienteTransaction
        {
            private readonly IDbContextTransaction _transaction;

            public EfCompartilhamentoClienteTransaction(IDbContextTransaction transaction)
            {
                _transaction = transaction;
            }

            public Task CommitAsync()
            {
                return _transaction.CommitAsync();
            }

            public ValueTask DisposeAsync()
            {
                return _transaction.DisposeAsync();
            }
        }
    }
}
