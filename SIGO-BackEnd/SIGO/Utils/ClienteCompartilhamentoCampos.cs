using SIGO.Objects.Dtos.Entities;
using SIGO.Validation;
using System.Text.Json;

namespace SIGO.Utils
{
    public static class ClienteCompartilhamentoCampos
    {
        public const string Nome = "Nome";
        public const string Email = "Email";
        public const string CpfCnpj = "Cpf_Cnpj";
        public const string Telefones = "Telefones";
        public const string Veiculos = "Veiculos";

        public static readonly IReadOnlyCollection<string> Todos = new[]
        {
            Nome,
            Email,
            CpfCnpj,
            Telefones,
            Veiculos
        };

        private static readonly Dictionary<string, string> AllowedFields = new(StringComparer.OrdinalIgnoreCase)
        {
            [Nome] = Nome,
            [Email] = Email,
            [CpfCnpj] = CpfCnpj,
            ["CpfCnpj"] = CpfCnpj,
            [Telefones] = Telefones,
            [Veiculos] = Veiculos
        };

        public static List<string> Normalizar(IEnumerable<string>? campos)
        {
            var normalizados = (campos ?? Enumerable.Empty<string>())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Select(c =>
                {
                    if (!AllowedFields.TryGetValue(c, out var campo))
                        throw new BusinessValidationException(new[] { new ValidationError(nameof(CriarCompartilhamentoClienteDTO.DadosPermitidos), $"Campo não permitido: {c}.") });

                    return campo;
                })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (normalizados.Count == 0)
                throw new BusinessValidationException(new[] { new ValidationError(nameof(CriarCompartilhamentoClienteDTO.DadosPermitidos), "Informe ao menos um campo para compartilhar.") });

            return normalizados;
        }

        public static string Serializar(IEnumerable<string> campos)
        {
            return JsonSerializer.Serialize(Normalizar(campos));
        }

        public static HashSet<string> Deserializar(string? dadosPermitidos)
        {
            if (string.IsNullOrWhiteSpace(dadosPermitidos))
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var campos = JsonSerializer.Deserialize<List<string>>(dadosPermitidos) ?? new();
                if (campos.Count == 0)
                    return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                return Normalizar(campos).ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex is JsonException or BusinessValidationException)
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
