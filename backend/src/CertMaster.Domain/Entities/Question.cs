using CertMaster.Domain.Common;
using CertMaster.Domain.Enums;

namespace CertMaster.Domain.Entities;

public class Question : BaseEntity
{
    public Guid CertificationId { get; set; }
    public Certification? Certification { get; set; }
    public Guid QuestionBankVersionId { get; set; }
    public QuestionBankVersion? QuestionBankVersion { get; set; }

    public string Topic { get; set; } = string.Empty;
    public string? Subtopic { get; set; }
    public Difficulty Difficulty { get; set; } = Difficulty.Medium;

    public string Prompt { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string? ImageUrl { get; set; }

    public QuestionStatus Status { get; set; } = QuestionStatus.Draft;

    public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
}

public class QuestionOption : BaseEntity
{
    public Guid QuestionId { get; set; }
    public Question? Question { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int SortOrder { get; set; }
}

public class QuestionReport : BaseEntity
{
    public Guid QuestionId { get; set; }
    public Question? Question { get; set; }
    public Guid ReportedByUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool Resolved { get; set; }
}

public class Bookmark : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid QuestionId { get; set; }
    public Question? Question { get; set; }
}
