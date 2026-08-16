namespace SIGO.Security
{
    public sealed class PasswordRecoveryOptions
    {
        public const string SectionName = "PasswordRecovery";

        public string FrontendBaseUrl { get; init; } = string.Empty;
        public int TokenLifetimeMinutes { get; init; } = 30;

        public static bool IsValid(PasswordRecoveryOptions options)
        {
            return options is not null &&
                   options.TokenLifetimeMinutes is >= 5 and <= 1440 &&
                   Uri.TryCreate(
                       options.FrontendBaseUrl,
                       UriKind.Absolute,
                       out var frontendUri) &&
                   (frontendUri.Scheme == Uri.UriSchemeHttp ||
                    frontendUri.Scheme == Uri.UriSchemeHttps);
        }
    }
}
