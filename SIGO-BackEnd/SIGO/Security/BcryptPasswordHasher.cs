using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SIGO.Security
{
    public partial class BcryptPasswordHasher : IPasswordHasher
    {
        private const int WorkFactor = 12;

        public string Hash(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Password cannot be empty.", nameof(input));

            return BCrypt.Net.BCrypt.HashPassword(input, WorkFactor);
        }

        public bool Verify(string input, string hashedValue)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(hashedValue))
                return false;

            if (IsBcryptHash(hashedValue))
            {
                try
                {
                    return BCrypt.Net.BCrypt.Verify(input, hashedValue);
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return IsLegacySha256Hash(hashedValue) &&
                   CryptographicOperations.FixedTimeEquals(
                       Encoding.UTF8.GetBytes(HashSha256(input)),
                       Encoding.UTF8.GetBytes(hashedValue));
        }

        public bool NeedsRehash(string hashedValue)
        {
            if (!IsBcryptHash(hashedValue))
                return true;

            return !hashedValue.StartsWith($"$2a${WorkFactor:00}$", StringComparison.Ordinal) &&
                   !hashedValue.StartsWith($"$2b${WorkFactor:00}$", StringComparison.Ordinal) &&
                   !hashedValue.StartsWith($"$2y${WorkFactor:00}$", StringComparison.Ordinal);
        }

        private static bool IsBcryptHash(string value)
        {
            return value.StartsWith("$2a$", StringComparison.Ordinal) ||
                   value.StartsWith("$2b$", StringComparison.Ordinal) ||
                   value.StartsWith("$2y$", StringComparison.Ordinal);
        }

        private static bool IsLegacySha256Hash(string value) =>
            Sha256Regex().IsMatch(value);

        private static string HashSha256(string input)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            var builder = new StringBuilder(bytes.Length * 2);

            foreach (var b in bytes)
                builder.Append(b.ToString("x2"));

            return builder.ToString();
        }

        [GeneratedRegex("^[a-fA-F0-9]{64}$")]
        private static partial Regex Sha256Regex();
    }
}
