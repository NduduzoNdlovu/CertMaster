namespace CertMaster.Application.Features.Import;

public record StartImportRequest(Guid CertificationId, Guid? QuestionBankVersionId, string? NewVersionLabel);

public record QuestionBankVersionDto(
    Guid Id,
    Guid CertificationId,
    string VersionLabel,
    string Status,
    int QuestionsExtracted,
    int QuestionsFlagged,
    int DuplicatesDetected,
    DateTime CreatedAtUtc,
    DateTime? PublishedAtUtc);

public record ImportJobDto(
    Guid Id,
    Guid CertificationId,
    Guid QuestionBankVersionId,
    string VersionLabel,
    string OriginalFileName,
    string SourceFormat,
    string Status,
    string? ErrorMessage,
    int QuestionsExtracted,
    int QuestionsPendingReview,
    int QuestionsApproved,
    int QuestionsRejected,
    int DuplicatesDetected,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc);

public record ImportedQuestionOptionDto(Guid Id, string Text, bool IsCorrect, int SortOrder);

public record ImportedQuestionDto(
    Guid Id,
    string Topic,
    string? Subtopic,
    string Difficulty,
    string Prompt,
    string Explanation,
    string? Reference,
    string ReviewStatus,
    string ValidationIssues,
    bool IsDuplicate,
    Guid? DuplicateOfQuestionId,
    Guid? PublishedQuestionId,
    List<ImportedQuestionOptionDto> Options);

public record ImportJobImageDto(Guid Id, int PageNumber);

public record ImportJobDetailDto(ImportJobDto Job, List<ImportedQuestionDto> Questions, List<ImportJobImageDto> ExtractedImages);

public record UpdateImportedQuestionOptionRequest(Guid? Id, string Text, bool IsCorrect);

public record UpdateImportedQuestionRequest(
    string Topic,
    string? Subtopic,
    string Difficulty,
    string Prompt,
    string Explanation,
    string? Reference,
    List<UpdateImportedQuestionOptionRequest> Options);

public record ApproveQuestionsRequest(List<Guid> ImportedQuestionIds);

public record RejectQuestionsRequest(List<Guid> ImportedQuestionIds, string Reason);

public record PublishVersionResultDto(Guid VersionId, int QuestionsPublished, DateTime PublishedAtUtc);
