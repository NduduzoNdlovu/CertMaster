using CertMaster.Domain.Common;

namespace CertMaster.Domain.Entities;

/// <summary>CertMaster-issued recognition for a completed mock exam. This is not a vendor certification.</summary>
public sealed class AchievementCertificate : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid CertificationId { get; set; }
    public Certification? Certification { get; set; }
    public Guid ExamAttemptId { get; set; }
    public ExamAttempt? ExamAttempt { get; set; }
    public int ScorePercent { get; set; }
    public string Tier { get; set; } = string.Empty;
    public string VerificationCode { get; set; } = Guid.NewGuid().ToString("N").ToUpperInvariant();
    public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;
}
