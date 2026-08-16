using System.Net.Mail;

namespace SIGO.Integracao
{
    public sealed class EmailOptions
    {
        public const string SectionName = "Email";

        public string Host { get; init; } = string.Empty;
        public int Port { get; init; } = 587;
        public string? Username { get; init; }
        public string? Password { get; init; }
        public string FromAddress { get; init; } = string.Empty;
        public string FromName { get; init; } = "SIGO";
        public bool UseSsl { get; init; } = true;

        public static bool IsValid(EmailOptions options)
        {
            if (options is null ||
                string.IsNullOrWhiteSpace(options.Host) ||
                options.Port is < 1 or > 65535 ||
                !MailAddress.TryCreate(options.FromAddress, out _))
            {
                return false;
            }

            var hasUsername = !string.IsNullOrWhiteSpace(options.Username);
            var hasPassword = !string.IsNullOrWhiteSpace(options.Password);
            return hasUsername == hasPassword;
        }
    }
}
