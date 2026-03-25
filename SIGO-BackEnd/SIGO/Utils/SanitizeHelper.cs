using System.Linq;

namespace SIGO.Utils
{
    public static class SanitizeHelper
    {
        public static string ApenasDigitos(string? valor) =>
            string.IsNullOrWhiteSpace(valor)
                ? string.Empty
                : new string(valor.Where(char.IsDigit).ToArray());

        public static string TextoSemEspeciais(string? valor) =>
            string.IsNullOrWhiteSpace(valor)
                ? string.Empty
                : new string(valor.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray()).Trim();
    }
}
