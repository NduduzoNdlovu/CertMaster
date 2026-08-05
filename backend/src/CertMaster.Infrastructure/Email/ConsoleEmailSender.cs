using CertMaster.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CertMaster.Infrastructure.Email;

/// <summary>
/// Default email provider for local development and any environment without SMTP
/// credentials configured. Logs the email instead of sending it, so registration,
/// email verification, and password reset flows can be exercised end-to-end without
/// a real mail server. Switch Email:Provider to "Smtp" in configuration to send real
/// emails via SmtpEmailSender.
/// </summary>
public class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "\n----- EMAIL (dev mode — not actually sent) -----\nTo: {ToEmail}\nSubject: {Subject}\nBody:\n{Body}\n-------------------------------------------------",
            toEmail, subject, htmlBody);

        return Task.CompletedTask;
    }
}
