using System.Text;

namespace SIGO.Security
{
    public class JwtOptions
    {
        public const string SectionName = "Jwt";
        public const int MinimumKeyBytes = 32;

        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;

        public static bool IsValid(JwtOptions options)
        {
            return options is not null
                && !string.IsNullOrWhiteSpace(options.Key)
                && Encoding.UTF8.GetByteCount(options.Key) >= MinimumKeyBytes
                && !string.IsNullOrWhiteSpace(options.Issuer)
                && !string.IsNullOrWhiteSpace(options.Audience);
        }
    }
}
