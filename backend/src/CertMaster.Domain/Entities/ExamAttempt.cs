using CertMaster.Domain.Common;
using CertMaster.Domain.Enums;

namespace CertMaster.Domain.Entities;

public class ExamAttempt : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid CertificationId { get; set; }
    public Certification? Certification { get; set; }

    public ExamMode Mode { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public int DurationSeconds { get; set; }

    public int TotalQuestions { get; set; }
    public int CorrectCount { get; set; }
    public int Score { get; set; } // percentage 0-100
    public bool Passed { get; set; }

    public ICollection<ExamAnswer> Answers { get; set; } = new List<ExamAnswer>();
}

public class ExamAnswer : BaseEntity
{
    public Guid ExamAttemptId { get; set; }
    public ExamAttempt? ExamAttempt { get; set; }
    public Guid QuestionId { get; set; }
    public Question? Question { get; set; }
    public Guid? SelectedOptionId { get; set; }
    public bool IsCorrect { get; set; }
    public bool WasFlaggedForReview { get; set; }
}
