using System.Net;
using System.Net.Mail;
using CertMaster.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace CertMaster.Infrastructure.Email;

/// <summary>
/// Sends real email via SMTP. Activate by setting Email:Provider to "Smtp" and
/// filling in Email:SmtpHost / SmtpUsername / SmtpPassword (via user-secrets or
/// environment variables in any shared environment — never commit real credentials).
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;

    public SmtpEmailSender(IOptions<EmailSettings> options)
    {
        _settings = options.Value;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            EnableSsl = _settings.SmtpUseSsl,
            Credentials = new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword),
        };

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromAddress, _settings.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };
        message.To.Add(toEmail);

        await client.SendMailAsync(message, ct);
    }
}
