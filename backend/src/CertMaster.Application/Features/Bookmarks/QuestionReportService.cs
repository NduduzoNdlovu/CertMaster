using CertMaster.Application.Common.Interfaces;
using CertMaster.Domain.Entities;
using CertMaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CertMaster.Application.Features.Bookmarks;

public record ReportQuestionRequest(Guid QuestionId, string Reason);

public record QuestionReportDto(
    Guid Id,
    Guid QuestionId,
    string QuestionPrompt,
    string QuestionStatus,
    string ReportedByEmail,
    string Reason,
    bool Resolved,
    DateTime CreatedAtUtc);

/// <summary>"Republish" resolves the report and marks the question Published again (false alarm
/// or already fixed). "KeepFlagged" resolves the report but leaves the question Flagged so it
/// stays out of practice/mock exams until an admin edits and republishes it separately.</summary>
public record ResolveReportRequest(string Action = "Republish");

public class QuestionReportService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public QuestionReportService(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Records the report and immediately flags the question, pulling it out of the
    /// Published pool (and therefore out of practice/mock exams) until an admin reviews it.
    /// This favors learner safety over availability — a wrongly-flagged question costs a
    /// few minutes of admin review; a wrong answer served to hundreds of learners doesn't.
    /// </summary>
    public async Task ReportAsync(ReportQuestionRequest request, CancellationToken ct)
    {
        if (_currentUser.UserId is null) throw new UnauthorizedAccessException();

        var question = await _db.Questions.FirstOrDefaultAsync(q => q.Id == request.QuestionId, ct);
        if (question is null) throw new InvalidOperationException("Question not found.");

        _db.QuestionReports.Add(new QuestionReport
        {
            QuestionId = request.QuestionId,
            ReportedByUserId = _currentUser.UserId.Value,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? "No reason provided" : request.Reason.Trim(),
        });

        question.Status = QuestionStatus.Flagged;

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Admin view of all unresolved reports, most recent first.</summary>
    public async Task<List<QuestionReportDto>> GetOpenReportsAsync(CancellationToken ct)
    {
        var query =
            from r in _db.QuestionReports
            join q in _db.Questions on r.QuestionId equals q.Id
            join u in _db.Users on r.ReportedByUserId equals u.Id into userJoin
            from u in userJoin.DefaultIfEmpty()
            where !r.Resolved
            orderby r.CreatedAtUtc descending
            select new QuestionReportDto(
                r.Id,
                r.QuestionId,
                q.Prompt,
                q.Status.ToString(),
                u != null ? u.Email : "unknown",
                r.Reason,
                r.Resolved,
                r.CreatedAtUtc);

        return await query.Take(100).ToListAsync(ct);
    }

    public async Task ResolveAsync(Guid reportId, ResolveReportRequest request, CancellationToken ct)
    {
        var report = await _db.QuestionReports
            .Include(r => r.Question)
            .FirstOrDefaultAsync(r => r.Id == reportId, ct);
        if (report is null) return;

        report.Resolved = true;

        if (report.Question is not null)
        {
            report.Question.Status = string.Equals(request.Action, "KeepFlagged", StringComparison.OrdinalIgnoreCase)
                ? QuestionStatus.Flagged
                : QuestionStatus.Published;
        }

        await _db.SaveChangesAsync(ct);
    }
}
