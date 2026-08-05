using CertMaster.Application.Common;
using CertMaster.Application.Common.Interfaces;
using CertMaster.Application.Features.Certifications;
using CertMaster.Application.Features.DumpUpload;
using CertMaster.Domain.Entities;
using CertMaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CertMaster.Application.Features.Import;

public class ImportService
{
    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50MB
    private static readonly Dictionary<string, DumpSourceFormat> ExtensionToFormat = new()
    {
        [".pdf"] = DumpSourceFormat.Pdf,
        [".docx"] = DumpSourceFormat.Word,
        [".xlsx"] = DumpSourceFormat.Excel,
        [".csv"] = DumpSourceFormat.Csv,
        [".txt"] = DumpSourceFormat.Txt,
    };

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IFileStorage _fileStorage;
    private readonly IEnumerable<IDumpParser> _parsers;
    private readonly CertificationService _certificationService;

    public ImportService(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IFileStorage fileStorage,
        IEnumerable<IDumpParser> parsers,
        CertificationService certificationService)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _fileStorage = fileStorage;
        _parsers = parsers;
        _certificationService = certificationService;
    }

    // ---------------------------------------------------------------- Versions

    public async Task<List<QuestionBankVersionDto>> GetVersionsAsync(Guid certificationId, CancellationToken ct)
    {
        return await _db.QuestionBankVersions
            .Where(v => v.CertificationId == certificationId)
            .OrderByDescending(v => v.CreatedAtUtc)
            .Select(v => new QuestionBankVersionDto(
                v.Id, v.CertificationId, v.VersionLabel, v.Status.ToString(),
                v.QuestionsExtracted, v.QuestionsFlagged, v.DuplicatesDetected,
                v.CreatedAtUtc, v.PublishedAtUtc))
            .ToListAsync(ct);
    }

    // ---------------------------------------------------------------- Start import

    public async Task<ImportJobDto> StartImportAsync(
        StartImportRequest request, Stream fileStream, string fileName, CancellationToken ct)
    {
        var certification = await _db.Certifications.FirstOrDefaultAsync(c => c.Id == request.CertificationId, ct)
            ?? throw new InvalidOperationException("Certification not found.");

        // ---- Validate the uploaded file ----
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!ExtensionToFormat.TryGetValue(extension, out var sourceFormat))
            throw new InvalidOperationException("Unsupported file type. Upload a PDF, DOCX, XLSX, CSV, or TXT file.");

        // Buffer the whole upload once — we need to read it multiple times (magic-byte
        // check, hashing/storage, then parsing) and 50MB comfortably fits in memory.
        using var buffer = new MemoryStream();
        await fileStream.CopyToAsync(buffer, ct);
        if (buffer.Length == 0)
            throw new InvalidOperationException("The uploaded file is empty.");
        if (buffer.Length > MaxFileSizeBytes)
            throw new InvalidOperationException("File exceeds the 50MB upload limit.");

        if (sourceFormat == DumpSourceFormat.Pdf)
        {
            buffer.Position = 0;
            var header = new byte[5];
            var read = await buffer.ReadAsync(header, 0, 5, ct);
            var isPdf = read == 5 && System.Text.Encoding.ASCII.GetString(header) == "%PDF-";
            if (!isPdf)
                throw new InvalidOperationException("This file doesn't look like a valid PDF (failed file-signature check).");
        }

        // ---- Resolve or create the question-bank version ----
        var version = await ResolveVersionAsync(certification, request, ct);

        // ---- Store the original file securely ----
        buffer.Position = 0;
        var stored = await _fileStorage.SaveAsync(buffer, fileName, ct);

        var job = new ImportJob
        {
            CertificationId = certification.Id,
            QuestionBankVersionId = version.Id,
            UploadedByUserId = _currentUser.UserId?.ToString() ?? "unknown",
            OriginalFileName = fileName,
            StoredFilePath = stored.StoragePath,
            FileSha256 = stored.Sha256Hash,
            FileSizeBytes = stored.SizeBytes,
            SourceFormat = sourceFormat,
            Status = ImportJobStatus.Uploaded,
        };
        _db.ImportJobs.Add(job);

        version.Status = QuestionBankVersionStatus.ImportInProgress;

        _db.AuditLogEntries.Add(new AuditLogEntry
        {
            ActorEmail = _currentUser.Email ?? "unknown",
            Action = "Started question-bank import",
            Target = $"{certification.Name} — {fileName}",
            Level = "Info",
        });

        await _db.SaveChangesAsync(ct);

        // ---- Run the extraction pipeline ----
        try
        {
            await RunPipelineAsync(job, version, buffer, sourceFormat, extension, ct);
        }
        catch (Exception ex)
        {
            job.Status = ImportJobStatus.Failed;
            job.ErrorMessage = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
            job.CompletedAtUtc = _clock.UtcNow;

            _db.AuditLogEntries.Add(new AuditLogEntry
            {
                ActorEmail = _currentUser.Email ?? "unknown",
                Action = "Question-bank import failed",
                Target = $"{certification.Name} — {fileName}: {job.ErrorMessage}",
                Level = "Error",
            });

            await _db.SaveChangesAsync(ct);
        }

        return await ToJobDtoAsync(job, ct);
    }

    private async Task<QuestionBankVersion> ResolveVersionAsync(Certification certification, StartImportRequest request, CancellationToken ct)
    {
        if (request.QuestionBankVersionId is not null)
        {
            var existing = await _db.QuestionBankVersions.FirstOrDefaultAsync(
                v => v.Id == request.QuestionBankVersionId && v.CertificationId == certification.Id, ct)
                ?? throw new InvalidOperationException("Question-bank version not found for this certification.");

            if (existing.Status is QuestionBankVersionStatus.Published or QuestionBankVersionStatus.Archived)
                throw new InvalidOperationException(
                    "This version has already been published and can't receive new imports — start a new version instead.");

            return existing;
        }

        var versionCount = await _db.QuestionBankVersions.CountAsync(v => v.CertificationId == certification.Id, ct);
        var version = new QuestionBankVersion
        {
            CertificationId = certification.Id,
            Certification = certification,
            VersionLabel = string.IsNullOrWhiteSpace(request.NewVersionLabel)
                ? $"v{versionCount + 1} - {_clock.UtcNow:yyyy-MM-dd}"
                : request.NewVersionLabel.Trim(),
            CreatedByUserId = _currentUser.UserId?.ToString() ?? "unknown",
            Status = QuestionBankVersionStatus.Draft,
        };
        _db.QuestionBankVersions.Add(version);
        return version;
    }

    private async Task RunPipelineAsync(
        ImportJob job, QuestionBankVersion version, MemoryStream buffer, DumpSourceFormat sourceFormat, string extension, CancellationToken ct)
    {
        // ---- Extract text and images ----
        job.Status = ImportJobStatus.Extracting;
        await _db.SaveChangesAsync(ct);

        var parser = _parsers.FirstOrDefault(p => p.SupportedExtensions.Contains(extension))
            ?? throw new InvalidOperationException($"No parser registered for '{extension}' files.");

        buffer.Position = 0;
        var parsedQuestions = await parser.ParseAsync(buffer, ct);

        if (sourceFormat == DumpSourceFormat.Pdf)
        {
            buffer.Position = 0;
            await ExtractPdfImagesAsync(job, buffer, ct);
        }

        // ---- Detect question boundaries / options / correct answers / explanations ----
        // (all handled inside the parser — ParsedQuestion already reflects this)
        job.Status = ImportJobStatus.DetectingQuestions;
        await _db.SaveChangesAsync(ct);

        // ---- Normalise the extracted content ----
        job.Status = ImportJobStatus.Normalizing;
        foreach (var pq in parsedQuestions)
        {
            pq.Topic = TextNormalizer.NormalizeTopic(pq.Topic);
            pq.Subtopic = string.IsNullOrWhiteSpace(pq.Subtopic) ? null : TextNormalizer.NormalizeText(pq.Subtopic);
            pq.Prompt = TextNormalizer.NormalizeText(pq.Prompt);
            pq.Explanation = TextNormalizer.NormalizeText(pq.Explanation);
            pq.Reference = string.IsNullOrWhiteSpace(pq.Reference) ? null : TextNormalizer.NormalizeText(pq.Reference);
            for (var i = 0; i < pq.Options.Count; i++)
                pq.Options[i] = TextNormalizer.NormalizeText(pq.Options[i]);
        }
        await _db.SaveChangesAsync(ct);

        // ---- Detect duplicates and validation problems ----
        job.Status = ImportJobStatus.DetectingDuplicates;

        var existingQuestions = await _db.Questions
            .Where(q => q.CertificationId == job.CertificationId)
            .Select(q => new { q.Id, q.Prompt })
            .ToListAsync(ct);
        var existingByPrompt = existingQuestions
            .GroupBy(q => q.Prompt.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        var pendingImported = await _db.ImportedQuestions
            .Where(q => q.CertificationId == job.CertificationId && q.ReviewStatus != ImportedQuestionReviewStatus.Rejected)
            .Select(q => q.Prompt)
            .ToListAsync(ct);
        var pendingPromptSet = new HashSet<string>(pendingImported, StringComparer.OrdinalIgnoreCase);

        var seenInThisFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var pendingCount = 0;
        var duplicateCount = 0;
        var validationIssueCount = 0;

        foreach (var pq in parsedQuestions)
        {
            var issues = new List<string>();
            if (string.IsNullOrWhiteSpace(pq.Prompt)) issues.Add("No question text found.");
            var nonEmptyOptions = pq.Options.Count(o => !string.IsNullOrWhiteSpace(o));
            if (nonEmptyOptions < 2) issues.Add($"Only {nonEmptyOptions} answer option(s) found (need at least 2).");
            if (pq.CorrectOptionIndex < 0 || pq.CorrectOptionIndex >= pq.Options.Count)
                issues.Add("No correct answer could be identified.");
            if (pq.Options.Select(o => o.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() < pq.Options.Count)
                issues.Add("Two or more answer options are identical.");

            var isDuplicate = false;
            Guid? duplicateOfId = null;
            var normalizedPrompt = pq.Prompt?.Trim() ?? string.Empty;

            if (normalizedPrompt.Length > 0)
            {
                if (existingByPrompt.TryGetValue(normalizedPrompt, out var existingId))
                {
                    isDuplicate = true;
                    duplicateOfId = existingId;
                    issues.Add("Matches a question already in the live question bank.");
                }
                else if (pendingPromptSet.Contains(normalizedPrompt) || seenInThisFile.Contains(normalizedPrompt))
                {
                    isDuplicate = true;
                    issues.Add("Duplicate of another question already awaiting review.");
                }
                seenInThisFile.Add(normalizedPrompt);
            }

            if (isDuplicate) duplicateCount++;
            if (issues.Count > 0) validationIssueCount++;
            pendingCount++;

            var importedQuestion = new ImportedQuestion
            {
                ImportJobId = job.Id,
                QuestionBankVersionId = version.Id,
                CertificationId = job.CertificationId,
                Topic = pq.Topic ?? "General",
                Subtopic = pq.Subtopic,
                Difficulty = pq.ResolveDifficulty(),
                Prompt = normalizedPrompt,
                Explanation = pq.Explanation ?? string.Empty,
                Reference = pq.Reference,
                ReviewStatus = ImportedQuestionReviewStatus.PendingReview,
                ValidationIssues = string.Join(" ", issues),
                IsDuplicate = isDuplicate,
                DuplicateOfQuestionId = duplicateOfId,
            };

            for (var i = 0; i < pq.Options.Count; i++)
            {
                importedQuestion.Options.Add(new ImportedQuestionOption
                {
                    Text = pq.Options[i],
                    IsCorrect = i == pq.CorrectOptionIndex,
                    SortOrder = i + 1,
                });
            }

            _db.ImportedQuestions.Add(importedQuestion);
        }

        // ---- Ready for administrator review ----
        job.Status = ImportJobStatus.ReadyForReview;
        job.QuestionsExtracted = parsedQuestions.Count;
        job.QuestionsPendingReview = pendingCount;
        job.DuplicatesDetected = duplicateCount;
        job.ValidationIssuesCount = validationIssueCount;
        job.CompletedAtUtc = _clock.UtcNow;

        version.QuestionsExtracted += parsedQuestions.Count;
        version.DuplicatesDetected += duplicateCount;
        version.QuestionsFlagged += validationIssueCount;
        if (version.Status != QuestionBankVersionStatus.Published)
            version.Status = QuestionBankVersionStatus.PendingReview;

        _db.AuditLogEntries.Add(new AuditLogEntry
        {
            ActorEmail = _currentUser.Email ?? "unknown",
            Action = "Question-bank import ready for review",
            Target = $"{parsedQuestions.Count} extracted, {duplicateCount} duplicates, {validationIssueCount} flagged",
            Level = "Info",
        });

        await _db.SaveChangesAsync(ct);
    }

    private async Task ExtractPdfImagesAsync(ImportJob job, MemoryStream buffer, CancellationToken ct)
    {
        // Best-effort: extracted images are stored per import job for the admin to view
        // during review. See ImportJobImage for why they aren't auto-linked to a specific
        // question in this release.
        try
        {
            using var document = UglyToad.PdfPig.PdfDocument.Open(buffer);
            var pageNumber = 0;
            foreach (var page in document.GetPages())
            {
                pageNumber++;
                foreach (var image in page.GetImages())
                {
                    ct.ThrowIfCancellationRequested();
                    if (!image.TryGetPng(out var pngBytes)) continue;

                    using var imageStream = new MemoryStream(pngBytes);
                    var stored = await _fileStorage.SaveAsync(imageStream, $"page-{pageNumber}.png", ct);

                    _db.ImportJobImages.Add(new ImportJobImage
                    {
                        ImportJobId = job.Id,
                        StoredFilePath = stored.StoragePath,
                        PageNumber = pageNumber,
                    });
                }
            }
        }
        catch
        {
            // Image extraction is a best-effort enhancement — if it fails for a given PDF
            // (unsupported image encoding, etc.) the text-based question import still
            // proceeds normally.
        }
    }

    // ---------------------------------------------------------------- Review queue

    public async Task<List<ImportJobDto>> GetImportJobsAsync(Guid? certificationId, CancellationToken ct)
    {
        var query = _db.ImportJobs.Include(j => j.QuestionBankVersion).AsQueryable();
        if (certificationId is not null)
            query = query.Where(j => j.CertificationId == certificationId);

        var jobs = await query.OrderByDescending(j => j.CreatedAtUtc).Take(100).ToListAsync(ct);
        return jobs.Select(ToJobDto).ToList();
    }

    public async Task<ImportJobDetailDto> GetImportJobDetailAsync(Guid jobId, CancellationToken ct)
    {
        var job = await _db.ImportJobs
            .Include(j => j.QuestionBankVersion)
            .Include(j => j.ExtractedImages)
            .FirstOrDefaultAsync(j => j.Id == jobId, ct)
            ?? throw new InvalidOperationException("Import job not found.");

        var questions = await _db.ImportedQuestions
            .Include(q => q.Options)
            .Where(q => q.ImportJobId == jobId)
            .OrderBy(q => q.CreatedAtUtc)
            .ToListAsync(ct);

        return new ImportJobDetailDto(
            ToJobDto(job),
            questions.Select(ToQuestionDto).ToList(),
            job.ExtractedImages.Select(i => new ImportJobImageDto(i.Id, i.PageNumber)).ToList());
    }

    public async Task<ImportedQuestionDto> UpdateImportedQuestionAsync(Guid id, UpdateImportedQuestionRequest request, CancellationToken ct)
    {
        var question = await _db.ImportedQuestions
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id, ct)
            ?? throw new InvalidOperationException("Imported question not found.");

        if (question.PublishedQuestionId is not null)
            throw new InvalidOperationException("This question has already been published and can no longer be edited here.");

        question.Topic = TextNormalizer.NormalizeTopic(request.Topic);
        question.Subtopic = string.IsNullOrWhiteSpace(request.Subtopic) ? null : TextNormalizer.NormalizeText(request.Subtopic);
        question.Difficulty = request.Difficulty.Trim().ToLowerInvariant() switch
        {
            "easy" => Difficulty.Easy,
            "hard" => Difficulty.Hard,
            _ => Difficulty.Medium,
        };
        question.Prompt = TextNormalizer.NormalizeText(request.Prompt);
        question.Explanation = TextNormalizer.NormalizeText(request.Explanation);
        question.Reference = string.IsNullOrWhiteSpace(request.Reference) ? null : TextNormalizer.NormalizeText(request.Reference);

        // Replace options wholesale — simplest correct approach for admin corrections.
        _db.ImportedQuestionOptions.RemoveRange(question.Options);
        question.Options.Clear();
        for (var i = 0; i < request.Options.Count; i++)
        {
            question.Options.Add(new ImportedQuestionOption
            {
                Text = TextNormalizer.NormalizeText(request.Options[i].Text),
                IsCorrect = request.Options[i].IsCorrect,
                SortOrder = i + 1,
            });
        }

        // Re-run basic validation now that the admin has corrected the content.
        var issues = new List<string>();
        var nonEmptyOptions = question.Options.Count(o => !string.IsNullOrWhiteSpace(o.Text));
        if (string.IsNullOrWhiteSpace(question.Prompt)) issues.Add("No question text found.");
        if (nonEmptyOptions < 2) issues.Add($"Only {nonEmptyOptions} answer option(s) found (need at least 2).");
        if (!question.Options.Any(o => o.IsCorrect)) issues.Add("No correct answer marked.");
        question.ValidationIssues = string.Join(" ", issues);

        // A corrected question goes back to pending review even if it had been
        // previously rejected — the admin is explicitly asking for it to be reconsidered.
        if (question.ReviewStatus == ImportedQuestionReviewStatus.Rejected)
            question.ReviewStatus = ImportedQuestionReviewStatus.PendingReview;

        await _db.SaveChangesAsync(ct);
        await RefreshJobCountsAsync(new[] { question.ImportJobId }, ct);

        return ToQuestionDto(question);
    }

    public async Task ApproveAsync(ApproveQuestionsRequest request, CancellationToken ct)
    {
        var questions = await _db.ImportedQuestions
            .Include(q => q.Options)
            .Where(q => request.ImportedQuestionIds.Contains(q.Id))
            .ToListAsync(ct);

        foreach (var q in questions)
        {
            if (q.PublishedQuestionId is not null) continue; // already published, immutable

            var hasCorrectAnswer = q.Options.Any(o => o.IsCorrect);
            var hasEnoughOptions = q.Options.Count(o => !string.IsNullOrWhiteSpace(o.Text)) >= 2;
            if (string.IsNullOrWhiteSpace(q.Prompt) || !hasEnoughOptions || !hasCorrectAnswer)
                throw new InvalidOperationException(
                    $"Can't approve a question with missing text, fewer than 2 options, or no correct answer marked (question: \"{Truncate(q.Prompt, 50)}\").");

            q.ReviewStatus = ImportedQuestionReviewStatus.Approved;
            q.ReviewedByUserId = _currentUser.UserId;
            q.ReviewedAtUtc = _clock.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        await RefreshJobCountsAsync(questions.Select(q => q.ImportJobId).Distinct(), ct);
    }

    public async Task RejectAsync(RejectQuestionsRequest request, CancellationToken ct)
    {
        var questions = await _db.ImportedQuestions
            .Where(q => request.ImportedQuestionIds.Contains(q.Id) && q.PublishedQuestionId == null)
            .ToListAsync(ct);

        foreach (var q in questions)
        {
            q.ReviewStatus = ImportedQuestionReviewStatus.Rejected;
            q.ReviewedByUserId = _currentUser.UserId;
            q.ReviewedAtUtc = _clock.UtcNow;
            q.ReviewNotes = string.IsNullOrWhiteSpace(request.Reason) ? "No reason provided" : request.Reason.Trim();
        }

        await _db.SaveChangesAsync(ct);
        await RefreshJobCountsAsync(questions.Select(q => q.ImportJobId).Distinct(), ct);
    }

    /// <summary>Recomputes the cached review-status counts on each affected ImportJob so
    /// the job list (which reads the cached fields, not a live join) stays accurate
    /// without needing to re-query ImportedQuestions on every list request.</summary>
    private async Task RefreshJobCountsAsync(IEnumerable<Guid> jobIds, CancellationToken ct)
    {
        foreach (var jobId in jobIds)
        {
            var job = await _db.ImportJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);
            if (job is null) continue;

            job.QuestionsPendingReview = await _db.ImportedQuestions.CountAsync(
                q => q.ImportJobId == jobId && q.ReviewStatus == ImportedQuestionReviewStatus.PendingReview, ct);
            job.QuestionsApproved = await _db.ImportedQuestions.CountAsync(
                q => q.ImportJobId == jobId && q.ReviewStatus == ImportedQuestionReviewStatus.Approved, ct);
            job.QuestionsRejected = await _db.ImportedQuestions.CountAsync(
                q => q.ImportJobId == jobId && q.ReviewStatus == ImportedQuestionReviewStatus.Rejected, ct);
        }

        await _db.SaveChangesAsync(ct);
    }

    // ---------------------------------------------------------------- Publish

    public async Task<PublishVersionResultDto> PublishVersionAsync(Guid versionId, CancellationToken ct)
    {
        var version = await _db.QuestionBankVersions.FirstOrDefaultAsync(v => v.Id == versionId, ct)
            ?? throw new InvalidOperationException("Question-bank version not found.");

        if (version.Status == QuestionBankVersionStatus.Published)
            throw new InvalidOperationException("This version has already been published.");

        var stillPending = await _db.ImportedQuestions
            .AnyAsync(q => q.QuestionBankVersionId == versionId && q.ReviewStatus == ImportedQuestionReviewStatus.PendingReview, ct);
        if (stillPending)
            throw new InvalidOperationException(
                "Every imported question in this version must be approved or rejected before it can be published.");

        var approvedQuestions = await _db.ImportedQuestions
            .Include(q => q.Options)
            .Where(q => q.QuestionBankVersionId == versionId
                     && q.ReviewStatus == ImportedQuestionReviewStatus.Approved
                     && q.PublishedQuestionId == null)
            .ToListAsync(ct);

        foreach (var imported in approvedQuestions)
        {
            var question = new Question
            {
                CertificationId = imported.CertificationId,
                QuestionBankVersionId = version.Id,
                Topic = imported.Topic,
                Subtopic = imported.Subtopic,
                Difficulty = imported.Difficulty,
                Prompt = imported.Prompt,
                Explanation = imported.Explanation,
                Reference = imported.Reference,
                Status = QuestionStatus.Published,
            };

            foreach (var opt in imported.Options.OrderBy(o => o.SortOrder))
            {
                question.Options.Add(new QuestionOption
                {
                    Text = opt.Text,
                    IsCorrect = opt.IsCorrect,
                    SortOrder = opt.SortOrder,
                });
            }

            _db.Questions.Add(question);
            imported.PublishedQuestionId = question.Id;
        }

        version.Status = QuestionBankVersionStatus.Published;
        version.PublishedAtUtc = _clock.UtcNow;
        version.PublishedByUserId = _currentUser.Email ?? _currentUser.UserId?.ToString();

        _db.AuditLogEntries.Add(new AuditLogEntry
        {
            ActorEmail = _currentUser.Email ?? "unknown",
            Action = "Published question-bank version",
            Target = $"{version.VersionLabel} — {approvedQuestions.Count} questions",
            Level = "Info",
        });

        await _db.SaveChangesAsync(ct);
        await _certificationService.InvalidateCacheAsync(ct);

        return new PublishVersionResultDto(version.Id, approvedQuestions.Count, version.PublishedAtUtc.Value);
    }

    public async Task<Stream> OpenImportJobImageAsync(Guid imageId, CancellationToken ct)
    {
        var image = await _db.ImportJobImages.FirstOrDefaultAsync(i => i.Id == imageId, ct)
            ?? throw new InvalidOperationException("Image not found.");
        return await _fileStorage.OpenReadAsync(image.StoredFilePath, ct);
    }

    // ---------------------------------------------------------------- Mapping helpers

    private async Task<ImportJobDto> ToJobDtoAsync(ImportJob job, CancellationToken ct)
    {
        // Refresh review counts from the database in case questions were staged.
        var pending = await _db.ImportedQuestions.CountAsync(q => q.ImportJobId == job.Id && q.ReviewStatus == ImportedQuestionReviewStatus.PendingReview, ct);
        var approved = await _db.ImportedQuestions.CountAsync(q => q.ImportJobId == job.Id && q.ReviewStatus == ImportedQuestionReviewStatus.Approved, ct);
        var rejected = await _db.ImportedQuestions.CountAsync(q => q.ImportJobId == job.Id && q.ReviewStatus == ImportedQuestionReviewStatus.Rejected, ct);
        var version = await _db.QuestionBankVersions.FirstAsync(v => v.Id == job.QuestionBankVersionId, ct);

        return new ImportJobDto(
            job.Id, job.CertificationId, job.QuestionBankVersionId, version.VersionLabel,
            job.OriginalFileName, job.SourceFormat.ToString(), job.Status.ToString(), job.ErrorMessage,
            job.QuestionsExtracted, pending, approved, rejected, job.DuplicatesDetected,
            job.CreatedAtUtc, job.CompletedAtUtc);
    }

    private static ImportJobDto ToJobDto(ImportJob job)
    {
        return new ImportJobDto(
            job.Id, job.CertificationId, job.QuestionBankVersionId, job.QuestionBankVersion?.VersionLabel ?? string.Empty,
            job.OriginalFileName, job.SourceFormat.ToString(), job.Status.ToString(), job.ErrorMessage,
            job.QuestionsExtracted, job.QuestionsPendingReview, job.QuestionsApproved, job.QuestionsRejected,
            job.DuplicatesDetected, job.CreatedAtUtc, job.CompletedAtUtc);
    }

    private static ImportedQuestionDto ToQuestionDto(ImportedQuestion q)
    {
        return new ImportedQuestionDto(
            q.Id, q.Topic, q.Subtopic, q.Difficulty.ToString(), q.Prompt, q.Explanation, q.Reference,
            q.ReviewStatus.ToString(), q.ValidationIssues, q.IsDuplicate, q.DuplicateOfQuestionId, q.PublishedQuestionId,
            q.Options.OrderBy(o => o.SortOrder).Select(o => new ImportedQuestionOptionDto(o.Id, o.Text, o.IsCorrect, o.SortOrder)).ToList());
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...";
}
