using CertMaster.Domain.Common;

namespace CertMaster.Domain.Entities;

/// <summary>
/// A certification track (e.g. CompTIA A+ Core 1). New certifications are added
/// entirely as data — no code changes are required to introduce a new one.
/// </summary>
public class Certification : BaseEntity
{
    public string Code { get; set; } = string.Empty; // e.g. "220-1201"
    public string Name { get; set; } = string.Empty; // e.g. "CompTIA A+ Core 1"
    public string Vendor { get; set; } = string.Empty; // e.g. "CompTIA"
    public string CurrentVersion { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int ExamDurationMinutes { get; set; } = 90;
    public int PassingScorePercent { get; set; } = 65;
    public int MockExamQuestionCount { get; set; } = 90;

    public ICollection<Topic> Topics { get; set; } = new List<Topic>();
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<QuestionBankVersion> Versions { get; set; } = new List<QuestionBankVersion>();
}

public class Topic : BaseEntity
{
    public Guid CertificationId { get; set; }
    public Certification? Certification { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ParentTopicName { get; set; } // subtopic support without a self-referencing FK
}

/// <summary>
/// A question-bank version is created once per import cycle and never overwritten —
/// existing published versions are permanent historical records. A version can receive
/// one or more import jobs (e.g. an admin uploads two files to build up the same
/// version) before being reviewed and published as a whole.
/// </summary>
public class QuestionBankVersion : BaseEntity
{
    public Guid CertificationId { get; set; }
    public Certification? Certification { get; set; }
    public string VersionLabel { get; set; } = string.Empty; // e.g. "v3 - 2026-07-30"
    public string CreatedByUserId { get; set; } = string.Empty;
    public Enums.QuestionBankVersionStatus Status { get; set; } = Enums.QuestionBankVersionStatus.Draft;

    public int QuestionsExtracted { get; set; }
    public int QuestionsFlagged { get; set; }
    public int DuplicatesDetected { get; set; }

    public DateTime? PublishedAtUtc { get; set; }
    public string? PublishedByUserId { get; set; }

    public ICollection<ImportJob> ImportJobs { get; set; } = new List<ImportJob>();
}
