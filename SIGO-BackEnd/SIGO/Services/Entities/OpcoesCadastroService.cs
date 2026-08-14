using SIGO.Data.Interfaces;
using SIGO.Objects.Dtos.Entities;
using SIGO.Objects.Models;
using SIGO.Services.Interfaces;

namespace SIGO.Services.Entities
{
    public class OpcoesCadastroService : IOpcoesCadastroService
    {
        private const string ModeloVeiculo = "modeloVeiculo";
        private const string Combustivel = "combustivel";
        private const string Cor = "cor";
        private const string Cargo = "cargo";
        private const string TipoMarca = "tipoMarca";
        private const string Fornecedor = "fornecedor";

        private readonly IOpcaoCadastroRepository _repository;

        public OpcoesCadastroService(IOpcaoCadastroRepository repository)
        {
            _repository = repository;
        }

        public async Task<OpcoesCadastroDTO> GetByOficinaAsync(
            int oficinaId,
            CancellationToken cancellationToken = default)
        {
            var options = await _repository.GetByOficinaAsync(oficinaId, cancellationToken);

            return new OpcoesCadastroDTO(
                GetValues(options, ModeloVeiculo),
                GetValues(options, Combustivel),
                GetValues(options, Cor),
                GetValues(options, Cargo),
                GetValues(options, TipoMarca),
                GetValues(options, Fornecedor));
        }

        private static IReadOnlyList<string> GetValues(
            IEnumerable<OpcaoCadastro> options,
            string category)
        {
            return options
                .Where(option => string.Equals(
                    option.Categoria,
                    category,
                    StringComparison.Ordinal))
                .Select(option => option.Valor.Trim())
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }
}
