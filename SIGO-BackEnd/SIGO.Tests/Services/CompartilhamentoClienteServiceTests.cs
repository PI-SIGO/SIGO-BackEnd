using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SIGO.Data.Interfaces;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;
using SIGO.Security;
using SIGO.Services.Entities;
using Xunit;

namespace SIGO.Tests.Services
{
    public class CompartilhamentoClienteServiceTests
    {
        [Fact]
        public async Task ResgatarCodigo_DevePersistirTentativaFalha_QuandoCodigoIndisponivel()
        {
            var repository = new FakeCompartilhamentoClienteRepository();
            var service = CreateService(repository);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.ResgatarCodigo(7, new ResgatarCompartilhamentoClienteDTO { Codigo = "123456" }, "203.0.113.10"));

            var tentativasPersistidas = repository.TentativasPersistidas;
            Assert.Single(tentativasPersistidas);
            Assert.False(tentativasPersistidas[0].Sucesso);
            Assert.Equal("Indisponivel", tentativasPersistidas[0].Motivo);
            Assert.Equal(0, repository.TransactionsCommitted);
            Assert.Equal(1, repository.TransactionsRolledBack);
            Assert.Equal(1, await repository.CountFalhasRecentesAsync(7, "203.0.113.10", DateTime.UtcNow.AddMinutes(-15)));
        }

        [Fact]
        public async Task ResgatarCodigo_DeveBloquearDepoisDeFalhasPersistidasMesmoComExcecoes()
        {
            var repository = new FakeCompartilhamentoClienteRepository();
            var service = CreateService(repository);

            for (var tentativa = 0; tentativa < 10; tentativa++)
            {
                await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                    service.ResgatarCodigo(7, new ResgatarCompartilhamentoClienteDTO { Codigo = "123456" }, "203.0.113.10"));
            }

            var chamadasResgateAntesDoBloqueio = repository.RedeemValidCalls;

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.ResgatarCodigo(7, new ResgatarCompartilhamentoClienteDTO { Codigo = "123456" }, "203.0.113.10"));

            Assert.Equal(10, repository.TentativasPersistidas.Count);
            Assert.Equal(10, await repository.CountFalhasRecentesAsync(7, "203.0.113.10", DateTime.UtcNow.AddMinutes(-15)));
            Assert.Equal(chamadasResgateAntesDoBloqueio, repository.RedeemValidCalls);
            Assert.Equal(0, repository.TransactionsCommitted);
            Assert.Equal(10, repository.TransactionsRolledBack);
        }

        [Fact]
        public async Task ResgatarCodigo_DeveAplicarBloqueioParaCodigoComFormatoInvalido()
        {
            var repository = new FakeCompartilhamentoClienteRepository();
            var service = CreateService(repository);

            for (var tentativa = 0; tentativa < 10; tentativa++)
            {
                await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                    service.ResgatarCodigo(7, new ResgatarCompartilhamentoClienteDTO { Codigo = "abc" }, "203.0.113.10"));
            }

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.ResgatarCodigo(7, new ResgatarCompartilhamentoClienteDTO { Codigo = "abc" }, "203.0.113.10"));

            Assert.Equal(10, repository.TentativasPersistidas.Count);
            Assert.Equal(10, await repository.CountFalhasRecentesAsync(7, "203.0.113.10", DateTime.UtcNow.AddMinutes(-15)));
            Assert.Equal(0, repository.RedeemValidCalls);
            Assert.Equal(0, repository.TransactionsCommitted);
        }

        private static CompartilhamentoClienteService CreateService(FakeCompartilhamentoClienteRepository repository)
        {
            return new CompartilhamentoClienteService(
                repository,
                new FakeClienteOficinaRepository(),
                Options.Create(new CompartilhamentoClienteOptions
                {
                    CodigoHmacSecret = "12345678901234567890123456789012"
                }),
                NullLogger<CompartilhamentoClienteService>.Instance);
        }

        private sealed class FakeCompartilhamentoClienteRepository : ICompartilhamentoClienteRepository
        {
            private readonly List<CompartilhamentoClienteTentativa> _tentativasPersistidas = new();
            private List<CompartilhamentoClienteTentativa>? _tentativasEmTransacao;

            public int RedeemValidCalls { get; private set; }
            public int TransactionsCommitted { get; private set; }
            public int TransactionsRolledBack { get; private set; }

            public IReadOnlyList<CompartilhamentoClienteTentativa> TentativasPersistidas => _tentativasPersistidas;

            public Task<ICompartilhamentoClienteTransaction> BeginTransactionAsync()
            {
                if (_tentativasEmTransacao is not null)
                    throw new InvalidOperationException("Transacao de teste ja iniciada.");

                _tentativasEmTransacao = new List<CompartilhamentoClienteTentativa>();
                return Task.FromResult<ICompartilhamentoClienteTransaction>(new FakeTransaction(this));
            }

            public Task AddAsync(CompartilhamentoCliente compartilhamento)
            {
                throw new NotSupportedException();
            }

            public Task<bool> ExistsByCodeHashAsync(string codigoHash)
            {
                throw new NotSupportedException();
            }

            public Task<CompartilhamentoCliente?> GetByCodeHashAsync(string codigoHash)
            {
                throw new NotSupportedException();
            }

            public Task<CompartilhamentoCliente?> GetValidByCodeHashAsync(string codigoHash, DateTime agoraUtc)
            {
                throw new NotSupportedException();
            }

            public Task<CompartilhamentoCliente?> RedeemValidByCodeHashAsync(string codigoHash, DateTime agoraUtc)
            {
                RedeemValidCalls++;
                return Task.FromResult<CompartilhamentoCliente?>(null);
            }

            public Task AddTentativaAsync(CompartilhamentoClienteTentativa tentativa)
            {
                if (_tentativasEmTransacao is null)
                    _tentativasPersistidas.Add(tentativa);
                else
                    _tentativasEmTransacao.Add(tentativa);

                return Task.CompletedTask;
            }

            public Task<int> CountFalhasRecentesAsync(int oficinaId, string? ipAddress, DateTime desdeUtc)
            {
                var total = _tentativasPersistidas.Count(t =>
                    t.OficinaId == oficinaId &&
                    !t.Sucesso &&
                    t.TentadoEm >= desdeUtc &&
                    (ipAddress == null || t.IpAddress == ipAddress));

                return Task.FromResult(total);
            }

            public Task SaveChangesAsync()
            {
                return Task.CompletedTask;
            }

            private Task CommitAsync()
            {
                if (_tentativasEmTransacao is null)
                    throw new InvalidOperationException("Transacao de teste nao iniciada.");

                _tentativasPersistidas.AddRange(_tentativasEmTransacao);
                _tentativasEmTransacao = null;
                TransactionsCommitted++;

                return Task.CompletedTask;
            }

            private ValueTask DisposeTransactionAsync()
            {
                _tentativasEmTransacao = null;
                TransactionsRolledBack++;
                return ValueTask.CompletedTask;
            }

            private sealed class FakeTransaction : ICompartilhamentoClienteTransaction
            {
                private readonly FakeCompartilhamentoClienteRepository _repository;
                private bool _committed;

                public FakeTransaction(FakeCompartilhamentoClienteRepository repository)
                {
                    _repository = repository;
                }

                public async Task CommitAsync()
                {
                    await _repository.CommitAsync();
                    _committed = true;
                }

                public ValueTask DisposeAsync()
                {
                    return _committed ? ValueTask.CompletedTask : _repository.DisposeTransactionAsync();
                }
            }
        }

        private sealed class FakeClienteOficinaRepository : IClienteOficinaRepository
        {
            public Task<bool> ExistsAsync(int oficinaId, int clienteId)
            {
                throw new NotSupportedException();
            }

            public Task AddIfNotExistsAsync(int oficinaId, int clienteId)
            {
                throw new NotSupportedException();
            }

            public Task AddOrUpdatePermissoesAsync(int oficinaId, int clienteId, string dadosPermitidos)
            {
                throw new NotSupportedException();
            }
        }
    }
}
