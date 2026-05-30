using Microsoft.Extensions.Options;
using SIGO.Data.Interfaces;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;
using SIGO.Security;
using SIGO.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace SIGO.Services.Entities
{
    public class CompartilhamentoClienteService : ICompartilhamentoClienteService
    {
        private const string CodigoIndisponivelMessage = "Código inválido ou expirado.";
        private const int MaxTentativasFalhasRecentes = 10;
        private static readonly TimeSpan JanelaTentativasFalhas = TimeSpan.FromMinutes(15);

        private readonly ICompartilhamentoClienteRepository _compartilhamentoRepository;
        private readonly IClienteOficinaRepository _clienteOficinaRepository;
        private readonly ILogger<CompartilhamentoClienteService> _logger;
        private readonly byte[] _codigoHmacSecret;

        public CompartilhamentoClienteService(
            ICompartilhamentoClienteRepository compartilhamentoRepository,
            IClienteOficinaRepository clienteOficinaRepository,
            IOptions<CompartilhamentoClienteOptions> options,
            ILogger<CompartilhamentoClienteService> logger)
        {
            _compartilhamentoRepository = compartilhamentoRepository;
            _clienteOficinaRepository = clienteOficinaRepository;
            _logger = logger;
            if (string.IsNullOrWhiteSpace(options.Value.CodigoHmacSecret) ||
                Encoding.UTF8.GetByteCount(options.Value.CodigoHmacSecret) < 32)
                throw new InvalidOperationException("CompartilhamentoCliente:CodigoHmacSecret deve ter pelo menos 32 bytes.");

            _codigoHmacSecret = Encoding.UTF8.GetBytes(options.Value.CodigoHmacSecret);
        }

        public async Task<CompartilhamentoClienteCodigoDTO> CriarCodigo(int clienteId, CriarCompartilhamentoClienteDTO dto)
        {
            _ = dto;
            var codigo = await GerarCodigoUnico();
            var expiraEm = DateTime.UtcNow.AddMinutes(15);

            await _compartilhamentoRepository.AddAsync(new CompartilhamentoCliente
            {
                ClienteId = clienteId,
                CodigoHash = HashCodigo(codigo),
                ExpiraEm = expiraEm,
                Ativo = true
            });

            return new CompartilhamentoClienteCodigoDTO
            {
                Codigo = codigo,
                ExpiraEm = expiraEm
            };
        }

        public async Task<CompartilhamentoClienteResultadoDTO> ResgatarCodigo(int oficinaId, ResgatarCompartilhamentoClienteDTO dto, string? ipAddress = null)
        {
            var codigo = SomenteDigitos(dto?.Codigo);
            var codigoHash = HashCodigo(codigo);

            var falhasRecentes = await _compartilhamentoRepository.CountFalhasRecentesAsync(
                oficinaId,
                ipAddress,
                DateTime.UtcNow.Subtract(JanelaTentativasFalhas));

            if (falhasRecentes >= MaxTentativasFalhasRecentes)
            {
                _logger.LogWarning(
                    "Bloqueio temporario de resgate de compartilhamento por excesso de falhas. OficinaId={OficinaId} IpAddress={IpAddress} FalhasRecentes={FalhasRecentes}",
                    oficinaId,
                    ipAddress,
                    falhasRecentes);

                throw new KeyNotFoundException(CodigoIndisponivelMessage);
            }

            if (codigo.Length != 6)
            {
                await RegistrarTentativa(oficinaId, codigoHash, ipAddress, false, "FormatoInvalido");
                throw new KeyNotFoundException(CodigoIndisponivelMessage);
            }

            var agora = DateTime.UtcNow;
            await using (var transaction = await _compartilhamentoRepository.BeginTransactionAsync())
            {
                var compartilhamento = await _compartilhamentoRepository.RedeemValidByCodeHashAsync(codigoHash, agora);
                if (compartilhamento is not null)
                {
                    await _clienteOficinaRepository.AddOrUpdateVinculoAsync(
                        oficinaId,
                        compartilhamento.ClienteId);

                    await RegistrarTentativa(oficinaId, codigoHash, ipAddress, true, "Sucesso");
                    await transaction.CommitAsync();

                    return new CompartilhamentoClienteResultadoDTO
                    {
                        ClienteId = compartilhamento.ClienteId,
                        Dados = MapearDadosCompletos(compartilhamento.Cliente)
                    };
                }
            }

            await RegistrarTentativa(oficinaId, codigoHash, ipAddress, false, "Indisponivel");
            throw new KeyNotFoundException(CodigoIndisponivelMessage);
        }

        private async Task<string> GerarCodigoUnico()
        {
            for (var tentativa = 0; tentativa < 5; tentativa++)
            {
                var codigo = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
                if (!await _compartilhamentoRepository.ExistsByCodeHashAsync(HashCodigo(codigo)))
                    return codigo;
            }

            throw new InvalidOperationException("Não foi possível gerar um código único.");
        }

        private static Dictionary<string, object?> MapearDadosCompletos(Cliente cliente)
        {
            return new Dictionary<string, object?>
            {
                ["Nome"] = cliente.Nome,
                ["Email"] = cliente.Email,
                ["Cpf_Cnpj"] = cliente.Cpf_Cnpj,
                ["Obs"] = cliente.Obs,
                ["Razao"] = cliente.Razao,
                ["DataNasc"] = cliente.DataNasc,
                ["Numero"] = cliente.Numero,
                ["Rua"] = cliente.Rua,
                ["Cidade"] = cliente.Cidade,
                ["Cep"] = cliente.Cep,
                ["Bairro"] = cliente.Bairro,
                ["Estado"] = cliente.Estado,
                ["Pais"] = cliente.Pais,
                ["Complemento"] = cliente.Complemento,
                ["Sexo"] = cliente.Sexo,
                ["TipoCliente"] = cliente.TipoCliente,
                ["Situacao"] = cliente.Situacao,
                ["Telefones"] = cliente.Telefones.Select(t => new
                {
                    t.Id,
                    t.DDD,
                    t.Numero
                }).ToList(),
                ["Veiculos"] = cliente.Veiculos.Select(v => new
                {
                    v.Id,
                    v.NomeVeiculo,
                    v.TipoVeiculo,
                    v.PlacaVeiculo,
                    v.ChassiVeiculo,
                    v.AnoFab,
                    v.Quilometragem,
                    v.Combustivel,
                    v.Seguro,
                    v.Cor,
                    v.Status
                }).ToList()
            };
        }

        private async Task RegistrarTentativa(int oficinaId, string codigoHash, string? ipAddress, bool sucesso, string motivo)
        {
            await _compartilhamentoRepository.AddTentativaAsync(new CompartilhamentoClienteTentativa
            {
                OficinaId = oficinaId,
                CodigoHash = codigoHash,
                IpAddress = ipAddress,
                Sucesso = sucesso,
                Motivo = motivo,
                TentadoEm = DateTime.UtcNow
            });

            if (sucesso)
            {
                _logger.LogInformation(
                    "Resgate de compartilhamento de cliente concluido. OficinaId={OficinaId} IpAddress={IpAddress}",
                    oficinaId,
                    ipAddress);
            }
            else
            {
                _logger.LogWarning(
                    "Falha no resgate de compartilhamento de cliente. OficinaId={OficinaId} IpAddress={IpAddress} Motivo={Motivo}",
                    oficinaId,
                    ipAddress,
                    motivo);
            }
        }

        private string HashCodigo(string codigo)
        {
            using var hmac = new HMACSHA256(_codigoHmacSecret);
            var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(codigo));
            return Convert.ToHexString(bytes);
        }

        private static string SomenteDigitos(string? valor)
        {
            return new string((valor ?? string.Empty).Where(char.IsDigit).ToArray());
        }
    }
}
