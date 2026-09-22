using CertMaster.Domain.Common;
using CertMaster.Domain.Enums;

namespace CertMaster.Domain.Entities;

/// <summary>
/// One record per uploaded file. This is the traceability backbone of the import
/// workflow: every stage the file passes through (validated, extracted, questions
/// detected, duplicates checked, ready for review — or failed, with a reason) is
/// recorded here, and every ImportedQuestion links back to the job that produced it.
/// </summary>
public class ImportJob : BaseEntity
{
    public Guid CertificationId { get; set; }
    public Certification? Certification { get; set; }
    public Guid QuestionBankVersionId { get; set; }
    public QuestionBankVersion? QuestionBankVersion { get; set; }

    public string UploadedByUserId { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;
    /// <summary>Path on secure server-side storage (never web-accessible) where the
    /// original file is retained, e.g. for re-processing or audit purposes.</summary>
    public string StoredFilePath { get; set; } = string.Empty;
    public string FileSha256 { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DumpSourceFormat SourceFormat { get; set; }

    public ImportJobStatus Status { get; set; } = ImportJobStatus.Uploaded;
    public string? ErrorMessage { get; set; }

    public int QuestionsExtracted { get; set; }
    public int QuestionsPendingReview { get; set; }
    public int QuestionsApproved { get; set; }
    public int QuestionsRejected { get; set; }
    public int DuplicatesDetected { get; set; }
    public int ValidationIssuesCount { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public ICollection<ImportedQuestion> ImportedQuestions { get; set; } = new List<ImportedQuestion>();
    public ICollection<ImportJobImage> ExtractedImages { get; set; } = new List<ImportJobImage>();
}

/// <summary>
/// An image extracted from an uploaded file (currently PDF only). Stored and made
/// available to the admin during review. Page ranges detected by the PDF parser are
/// used to associate each useful, non-repeated image with its imported question.
/// </summary>
public class ImportJobImage : BaseEntity
{
    public Guid ImportJobId { get; set; }
    public ImportJob? ImportJob { get; set; }
    public string StoredFilePath { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public Guid? ImportedQuestionId { get; set; }
    public ImportedQuestion? ImportedQuestion { get; set; }
    public string ImageKind { get; set; } = "Question";
    public int SortOrder { get; set; }
}

/// <summary>
/// A question staged for admin review. This is intentionally a separate table from
/// the live Question table — nothing here is visible to Practice or Mock Exams. It
/// only becomes a real Question once an admin approves it AND publishes the version
/// it belongs to.
/// </summary>
public class ImportedQuestion : BaseEntity
{
    public Guid ImportJobId { get; set; }
    public ImportJob? ImportJob { get; set; }
    public Guid QuestionBankVersionId { get; set; }
    public QuestionBankVersion? QuestionBankVersion { get; set; }
    public Guid CertificationId { get; set; }
    public Certification? Certification { get; set; }

    public string Topic { get; set; } = string.Empty;
    public string? Subtopic { get; set; }
    public Difficulty Difficulty { get; set; } = Difficulty.Medium;

    public string Prompt { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string QuestionType { get; set; } = "Choice";
    public bool RequiresManualReview { get; set; }
    public int? SourcePageStart { get; set; }
    public int? SourcePageEnd { get; set; }

    public ImportedQuestionReviewStatus ReviewStatus { get; set; } = ImportedQuestionReviewStatus.PendingReview;

    /// <summary>Human-readable validation problems found during import, e.g. "No option
    /// marked correct", "Only 1 option found". Empty when the question looked clean.</summary>
    public string ValidationIssues { get; set; } = string.Empty;

    public bool IsDuplicate { get; set; }
    public Guid? DuplicateOfQuestionId { get; set; }

    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewNotes { get; set; }

    /// <summary>Set once this imported question has been copied into the live Question
    /// table as part of publishing its version — the audit trail from source file to
    /// live question stays intact even after publish.</summary>
    public Guid? PublishedQuestionId { get; set; }

    public ICollection<ImportedQuestionOption> Options { get; set; } = new List<ImportedQuestionOption>();
    public ICollection<ImportJobImage> Images { get; set; } = new List<ImportJobImage>();
}

public class ImportedQuestionOption : BaseEntity
{
    public Guid ImportedQuestionId { get; set; }
    public ImportedQuestion? ImportedQuestion { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int SortOrder { get; set; }
}
