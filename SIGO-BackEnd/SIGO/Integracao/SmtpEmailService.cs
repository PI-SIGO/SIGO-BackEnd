using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;
using SIGO.Integracao.Interfaces;

namespace SIGO.Integracao
{
    public sealed class SmtpEmailService : IEmailService
    {
        private readonly EmailOptions _options;

        public SmtpEmailService(IOptions<EmailOptions> options)
        {
            _options = options?.Value
                ?? throw new InvalidOperationException("Email options are not configured.");
        }

        public async Task SendAsync(
            EmailMessage message,
            CancellationToken cancellationToken = default)
        {
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName),
                Subject = message.Subject,
                SubjectEncoding = Encoding.UTF8,
                Body = message.HtmlBody,
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = true
            };
            mailMessage.To.Add(new MailAddress(message.To));

            using var smtpClient = new SmtpClient(_options.Host, _options.Port)
            {
                DeliveryMethod = SmtpDeliveryMethod.Network,
                EnableSsl = _options.UseSsl,
                UseDefaultCredentials = false
            };

            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                smtpClient.Credentials = new NetworkCredential(
                    _options.Username,
                    _options.Password);
            }

            await smtpClient.SendMailAsync(mailMessage, cancellationToken);
        }
    }
}
