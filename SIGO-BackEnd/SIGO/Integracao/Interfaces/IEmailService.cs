namespace SIGO.Integracao.Interfaces
{
    public sealed record EmailMessage(
        string To,
        string Subject,
        string HtmlBody);

    public interface IEmailService
    {
        Task SendAsync(
            EmailMessage message,
            CancellationToken cancellationToken = default);
    }
}
