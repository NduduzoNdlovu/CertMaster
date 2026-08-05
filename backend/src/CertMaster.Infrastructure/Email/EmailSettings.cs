namespace CertMaster.Infrastructure.Email;

public class EmailSettings
{
    public const string SectionName = "Email";

    /// <summary>"Console" logs emails to the console/log instead of sending them — the default
    /// for local development so you don't need real SMTP credentials to try the app.
    /// Set to "Smtp" and fill in the fields below to send real emails.</summary>
    public string Provider { get; set; } = "Console";

    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public bool SmtpUseSsl { get; set; } = true;

    public string FromAddress { get; set; } = "no-reply@certmaster.local";
    public string FromName { get; set; } = "CertMaster";
}
