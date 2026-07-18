namespace SIGO.Validation;

/// <summary>
/// Produces the canonical representation used to persist and compare identity e-mails.
/// </summary>
public static class EmailNormalizer
{
    /// <summary>
    /// Trims surrounding whitespace and applies invariant lower casing.
    /// </summary>
    public static string Normalize(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        return email.Trim().ToLowerInvariant();
    }
}
