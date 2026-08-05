namespace CertMaster.Domain.Enums;

public enum UserRole
{
    Learner = 0,
    Administrator = 1,
    // Reserved for future use — no code changes required elsewhere to activate these.
    Moderator = 2,
    Instructor = 3
}

public enum SubscriptionPlan
{
    Free = 0,
    PremiumMonthly = 1,
    PremiumYearly = 2
}

public enum Difficulty
{
    Easy = 0,
    Medium = 1,
    Hard = 2
}

public enum QuestionStatus
{
    Draft = 0,
    Published = 1,
    Flagged = 2
}

public enum ExamMode
{
    Practice = 0,
    MockExam = 1
}

public enum MaintenanceStatus
{
    Scheduled = 0,
    Active = 1,
    Completed = 2,
    Cancelled = 3
}

public enum DumpSourceFormat
{
    Pdf = 0,
    Word = 1,
    Excel = 2,
    Csv = 3,
    Txt = 4
}

/// <summary>
/// Lifecycle of a question-bank version. A version starts as Draft the moment it's
/// created (or selected) for an import, moves through the import/review stages, and
/// only becomes Published — visible to Practice and Mock Exams — once every imported
/// question in it has been reviewed and an administrator explicitly publishes it.
/// </summary>
public enum QuestionBankVersionStatus
{
    Draft = 0,
    ImportInProgress = 1,
    PendingReview = 2,
    Published = 3,
    Archived = 4
}

/// <summary>Tracks a single uploaded file through the extraction pipeline, so every
/// import is traceable end-to-end from upload to review-readiness (or failure).</summary>
public enum ImportJobStatus
{
    Uploaded = 0,
    Validating = 1,
    Extracting = 2,
    DetectingQuestions = 3,
    Normalizing = 4,
    DetectingDuplicates = 5,
    ReadyForReview = 6,
    Failed = 7
}

/// <summary>An imported question never reaches learners in this status alone — it must
/// be Approved, and its version must be explicitly Published, before it's copied into
/// the live Question table.</summary>
public enum ImportedQuestionReviewStatus
{
    PendingReview = 0,
    Approved = 1,
    Rejected = 2
}
