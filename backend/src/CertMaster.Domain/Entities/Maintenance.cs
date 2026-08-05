using CertMaster.Domain.Common;
using CertMaster.Domain.Enums;

namespace CertMaster.Domain.Entities;

public class MaintenanceWindow : BaseEntity
{
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Scheduled;
    public bool WasForced { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
}

public class AuditLogEntry : BaseEntity
{
    public string ActorEmail { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Target { get; set; }
    public string Level { get; set; } = "Info"; // Info, Warning, Error
}

public class PaymentTransaction : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Enums.SubscriptionPlan Plan { get; set; }
    public decimal AmountZar { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Paid, Refunded, Failed
    public string? ProviderReference { get; set; }
}

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Type { get; set; } = "General"; // General, ExamResult, Maintenance, Billing
    public bool IsRead { get; set; }
}
