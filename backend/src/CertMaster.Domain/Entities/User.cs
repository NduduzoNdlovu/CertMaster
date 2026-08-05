using CertMaster.Domain.Common;
using CertMaster.Domain.Enums;

namespace CertMaster.Domain.Entities;

public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Learner;
    public bool EmailConfirmed { get; set; }
    public string? EmailConfirmationToken { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiresAtUtc { get; set; }
    public bool IsSuspended { get; set; }

    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Free;
    public DateTime? PremiumExpiresAtUtc { get; set; }

    public int StudyStreakDays { get; set; }
    public DateTime? LastActivityAtUtc { get; set; }

    public bool DailyRemindersEnabled { get; set; } = true;
    public bool WeeklySummaryEnabled { get; set; } = true;
    public bool ProductUpdatesEnabled { get; set; } = true;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<ExamAttempt> ExamAttempts { get; set; } = new List<ExamAttempt>();
    public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
}

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public bool IsActive => RevokedAtUtc is null && DateTime.UtcNow < ExpiresAtUtc;
}
