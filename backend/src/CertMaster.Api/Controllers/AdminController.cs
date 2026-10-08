using CertMaster.Application.Common.Interfaces;
using CertMaster.Application.Features.Admin;
using CertMaster.Application.Features.Certifications;
using CertMaster.Application.Features.Notifications;
using CertMaster.Domain.Entities;
using CertMaster.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CertMaster.Api.Controllers;

[ApiController]
[Authorize(Roles = "Administrator")]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly AdminService _adminService;
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly CertificationService _certificationService;

    public AdminController(AdminService adminService, IApplicationDbContext db, ICurrentUserService currentUser, CertificationService certificationService)
    {
        _adminService = adminService;
        _db = db;
        _currentUser = currentUser;
        _certificationService = certificationService;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<AdminOverviewDto>> GetOverview(CancellationToken ct)
    {
        return Ok(await _adminService.GetOverviewAsync(ct));
    }

    [HttpGet("users")]
    public async Task<ActionResult> GetUsers([FromQuery] string? search, [FromQuery] string? role,
        [FromQuery] string? plan, [FromQuery] string? status, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(u => u.FullName.ToLower().Contains(term) || u.Email.ToLower().Contains(term));
        }

        if (Enum.TryParse<UserRole>(role, true, out var parsedRole)) query = query.Where(u => u.Role == parsedRole);
        if (Enum.TryParse<SubscriptionPlan>(plan, true, out var parsedPlan)) query = query.Where(u => u.Plan == parsedPlan);
        if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase)) query = query.Where(u => !u.IsSuspended);
        if (string.Equals(status, "Suspended", StringComparison.OrdinalIgnoreCase)) query = query.Where(u => u.IsSuspended);

        var totalCount = await query.CountAsync(ct);

        var users = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                Role = u.Role.ToString(),
                Plan = u.Plan.ToString(),
                u.IsSuspended,
                u.CreatedAtUtc,
            })
            .ToListAsync(ct);

        return Ok(new { items = users, page, pageSize, totalCount, totalPages = (int)Math.Ceiling(totalCount / (double)pageSize) });
    }

    [HttpGet("certifications")]
    public async Task<ActionResult> GetCertifications(CancellationToken ct)
    {
        var certifications = await _db.Certifications.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id, c.Code, c.Name, c.Vendor, Version = c.CurrentVersion,
                c.Description, c.IsActive, c.ExamDurationMinutes, c.PassingScorePercent,
                c.MockExamQuestionCount,
                TopicCount = c.Topics.Count,
                QuestionCount = c.Questions.Count(q => q.Status == QuestionStatus.Published)
            }).ToListAsync(ct);
        return Ok(certifications);
    }

    public record UpdateCertificationRequest(
        string Code, string Name, string Vendor, string Version, string Description,
        bool IsActive, int ExamDurationMinutes, int PassingScorePercent, int MockExamQuestionCount);

    [HttpPut("certifications/{id:guid}")]
    public async Task<ActionResult> UpdateCertification(Guid id, UpdateCertificationRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Certification code and name are required." });
        if (request.ExamDurationMinutes is < 1 or > 480 || request.PassingScorePercent is < 1 or > 100 || request.MockExamQuestionCount is < 1 or > 500)
            return BadRequest(new { error = "Duration must be 1-480 minutes, pass mark 1-100, and question count 1-500." });

        var certification = await _db.Certifications.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (certification is null) return NotFound();

        certification.Code = request.Code.Trim();
        certification.Name = request.Name.Trim();
        certification.Vendor = request.Vendor?.Trim() ?? string.Empty;
        certification.CurrentVersion = request.Version?.Trim() ?? string.Empty;
        certification.Description = request.Description?.Trim() ?? string.Empty;
        certification.IsActive = request.IsActive;
        certification.ExamDurationMinutes = request.ExamDurationMinutes;
        certification.PassingScorePercent = request.PassingScorePercent;
        certification.MockExamQuestionCount = request.MockExamQuestionCount;

        _db.AuditLogEntries.Add(new AuditLogEntry
        {
            ActorEmail = _currentUser.Email ?? "unknown",
            Action = "Updated certification settings",
            Target = certification.Code,
            Level = "Info"
        });
        await _db.SaveChangesAsync(ct);
        await _certificationService.InvalidateCacheAsync(ct);
        return NoContent();
    }

    [HttpGet("users/{id:guid}")]
    public async Task<ActionResult> GetUser(Guid id, CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking().Where(u => u.Id == id).Select(u => new
        {
            u.Id, u.FullName, u.Email, Role = u.Role.ToString(), Plan = u.Plan.ToString(), u.IsSuspended,
            u.EmailConfirmed, u.PremiumExpiresAtUtc, u.StudyStreakDays, u.LastActivityAtUtc, u.CreatedAtUtc,
            CompletedAttempts = u.ExamAttempts.Count(a => a.CompletedAtUtc != null),
            AverageScore = u.ExamAttempts.Where(a => a.CompletedAtUtc != null).Select(a => (double?)a.Score).Average() ?? 0
        }).FirstOrDefaultAsync(ct);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost("users/{id:guid}/suspend")]
    public async Task<ActionResult> SuspendUser(Guid id, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return NotFound();

        user.IsSuspended = true;
        await _db.SaveChangesAsync(ct);

        _db.AuditLogEntries.Add(new AuditLogEntry
        {
            ActorEmail = _currentUser.Email ?? "unknown",
            Action = "Suspended user account",
            Target = user.Email,
            Level = "Warning",
        });
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpPost("users/{id:guid}/reactivate")]
    public async Task<ActionResult> ReactivateUser(Guid id, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return NotFound();
        user.IsSuspended = false;
        _db.AuditLogEntries.Add(new AuditLogEntry { ActorEmail = _currentUser.Email ?? "unknown",
            Action = "Reactivated user account", Target = user.Email, Level = "Info" });
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("results")]
    public async Task<ActionResult> GetResults(
        [FromQuery] string? search, [FromQuery] Guid? certificationId, [FromQuery] string? mode,
        [FromQuery] bool? passed, [FromQuery] int page = 1, [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.ExamAttempts.AsNoTracking().Where(a => a.CompletedAtUtc != null);
        if (certificationId.HasValue) query = query.Where(a => a.CertificationId == certificationId.Value);
        if (Enum.TryParse<ExamMode>(mode, true, out var parsedMode)) query = query.Where(a => a.Mode == parsedMode);
        if (passed.HasValue) query = query.Where(a => a.Passed == passed.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(a => a.User!.FullName.ToLower().Contains(term) || a.User.Email.ToLower().Contains(term));
        }
        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(a => a.CompletedAtUtc).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new
            {
                a.Id, UserId = a.UserId, UserName = a.User!.FullName, UserEmail = a.User.Email,
                CertificationId = a.CertificationId, CertificationName = a.Certification!.Name,
                Mode = a.Mode.ToString(), a.StartedAtUtc, a.CompletedAtUtc, a.DurationSeconds,
                a.TotalQuestions, a.CorrectCount, a.Score, a.Passed
            }).ToListAsync(ct);
        return Ok(new { items, page, pageSize, totalCount, totalPages = (int)Math.Ceiling(totalCount / (double)pageSize) });
    }

    [HttpGet("results/{id:guid}")]
    public async Task<ActionResult> GetResult(Guid id, CancellationToken ct)
    {
        var attempt = await _db.ExamAttempts.AsNoTracking()
            .Include(a => a.User).Include(a => a.Certification)
            .Include(a => a.Answers).ThenInclude(a => a.Question)
            .FirstOrDefaultAsync(a => a.Id == id && a.CompletedAtUtc != null, ct);
        if (attempt is null) return NotFound();
        return Ok(new
        {
            attempt = new
            {
                attempt.Id, attempt.UserId, UserName = attempt.User!.FullName, UserEmail = attempt.User.Email,
                CertificationName = attempt.Certification!.Name, Mode = attempt.Mode.ToString(),
                attempt.StartedAtUtc, attempt.CompletedAtUtc, attempt.DurationSeconds,
                attempt.TotalQuestions, attempt.CorrectCount, attempt.Score, attempt.Passed
            },
            answers = attempt.Answers.Select(a => new
            {
                a.QuestionId, Prompt = a.Question!.Prompt, a.IsCorrect,
                a.SelectedOptionId, a.WasFlaggedForReview
            })
        });
    }

    [HttpGet("questions")]
    public async Task<ActionResult> GetQuestions(
        [FromQuery] string? search, [FromQuery] Guid? certificationId, [FromQuery] string? status,
        [FromQuery] string? difficulty, [FromQuery] int page = 1, [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.Questions.AsNoTracking();
        if (certificationId.HasValue) query = query.Where(q => q.CertificationId == certificationId.Value);
        if (Enum.TryParse<QuestionStatus>(status, true, out var parsedStatus)) query = query.Where(q => q.Status == parsedStatus);
        if (Enum.TryParse<Difficulty>(difficulty, true, out var parsedDifficulty)) query = query.Where(q => q.Difficulty == parsedDifficulty);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(q => q.Prompt.ToLower().Contains(term) || q.Topic.ToLower().Contains(term));
        }
        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderBy(q => q.Topic).ThenBy(q => q.CreatedAtUtc).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(q => new
            {
                q.Id, q.CertificationId, CertificationName = q.Certification!.Name, q.QuestionBankVersionId,
                q.Topic, q.Subtopic, Difficulty = q.Difficulty.ToString(), q.Prompt, q.Explanation, q.Reference,
                q.QuestionType, Status = q.Status.ToString(),
                Options = q.Options.OrderBy(o => o.SortOrder).Select(o => new { o.Id, o.Text, o.IsCorrect, o.SortOrder })
            }).ToListAsync(ct);
        return Ok(new { items, page, pageSize, totalCount, totalPages = (int)Math.Ceiling(totalCount / (double)pageSize) });
    }

    public record AdminQuestionOptionRequest(Guid? Id, string Text, bool IsCorrect, int SortOrder);
    public record UpdateAdminQuestionRequest(
        string Topic, string? Subtopic, Difficulty Difficulty, string Prompt, string Explanation,
        string? Reference, string QuestionType, QuestionStatus Status, List<AdminQuestionOptionRequest> Options);

    [HttpGet("questions/{id:guid}")]
    public async Task<ActionResult> GetQuestion(Guid id, CancellationToken ct)
    {
        var q = await _db.Questions.AsNoTracking().Include(x => x.Options).Include(x => x.Certification)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (q is null) return NotFound();
        return Ok(new { q.Id, q.CertificationId, CertificationName = q.Certification!.Name, q.QuestionBankVersionId,
            q.Topic, q.Subtopic, Difficulty = q.Difficulty.ToString(), q.Prompt, q.Explanation, q.Reference, q.QuestionType, Status = q.Status.ToString(),
            Options = q.Options.OrderBy(o => o.SortOrder).Select(o => new { o.Id, o.Text, o.IsCorrect, o.SortOrder }) });
    }

    [HttpPut("questions/{id:guid}")]
    public async Task<ActionResult> UpdateQuestion(Guid id, UpdateAdminQuestionRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt) || string.IsNullOrWhiteSpace(request.Topic))
            return BadRequest(new { error = "Topic and question text are required." });
        if (request.Options is null || request.Options.Count < 2)
            return BadRequest(new { error = "At least two answer options are required." });
        if (request.QuestionType is "Choice" or "MultipleResponse" && !request.Options.Any(o => o.IsCorrect))
            return BadRequest(new { error = "At least one correct option is required." });
        if (request.QuestionType == "Choice" && request.Options.Count(o => o.IsCorrect) != 1)
            return BadRequest(new { error = "Single-choice questions must have exactly one correct option." });

        var q = await _db.Questions.Include(x => x.Options).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (q is null) return NotFound();
        q.Topic = request.Topic.Trim(); q.Subtopic = string.IsNullOrWhiteSpace(request.Subtopic) ? null : request.Subtopic.Trim();
        q.Difficulty = request.Difficulty; q.Prompt = request.Prompt.Trim(); q.Explanation = request.Explanation?.Trim() ?? string.Empty;
        q.Reference = string.IsNullOrWhiteSpace(request.Reference) ? null : request.Reference.Trim();
        q.QuestionType = request.QuestionType; q.Status = request.Status;
        _db.QuestionOptions.RemoveRange(q.Options);
        q.Options = request.Options.Select((o, i) => new QuestionOption { Text = o.Text.Trim(), IsCorrect = o.IsCorrect, SortOrder = i + 1 }).ToList();
        _db.AuditLogEntries.Add(new AuditLogEntry { ActorEmail = _currentUser.Email ?? "unknown", Action = "Updated question", Target = q.Id.ToString(), Level = "Info" });
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    public record BroadcastNotificationRequest(string Title, string Body, string Type, List<Guid>? UserIds);

    [HttpPost("notifications/broadcast")]
    public async Task<ActionResult> BroadcastNotification(BroadcastNotificationRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
            return BadRequest(new { error = "Title and body are required." });
        var type = string.IsNullOrWhiteSpace(request.Type) ? "General" : request.Type.Trim();
        var users = _db.Users.Where(u => u.Role == UserRole.Learner && !u.IsSuspended);
        if (request.UserIds is { Count: > 0 }) users = users.Where(u => request.UserIds.Contains(u.Id));
        var userIds = await users.Select(u => u.Id).ToListAsync(ct);
        foreach (var userId in userIds) _db.Notifications.Add(NotificationService.Build(userId, request.Title.Trim(), request.Body.Trim(), type));
        _db.AuditLogEntries.Add(new AuditLogEntry { ActorEmail = _currentUser.Email ?? "unknown", Action = "Broadcast learner notification", Target = $"{userIds.Count} learner(s)", Level = "Info" });
        await _db.SaveChangesAsync(ct);
        return Ok(new { recipients = userIds.Count });
    }

    [HttpGet("maintenance")]
    public async Task<ActionResult> GetMaintenanceWindows(CancellationToken ct)
    {
        var windows = await _db.MaintenanceWindows
            .OrderBy(w => w.StartsAtUtc)
            .Select(w => new
            {
                w.Id,
                w.StartsAtUtc,
                w.EndsAtUtc,
                w.Reason,
                Status = w.Status.ToString(),
            })
            .ToListAsync(ct);

        return Ok(windows);
    }

    public record CreateMaintenanceWindowRequest(DateTime StartsAtUtc, DateTime EndsAtUtc, string Reason);

    [HttpPost("maintenance")]
    public async Task<ActionResult> ScheduleMaintenanceWindow(CreateMaintenanceWindowRequest request, CancellationToken ct)
    {
        if (request.EndsAtUtc <= request.StartsAtUtc)
        {
            return BadRequest(new { error = "End time must be after the start time." });
        }

        var window = new MaintenanceWindow
        {
            StartsAtUtc = request.StartsAtUtc,
            EndsAtUtc = request.EndsAtUtc,
            Reason = request.Reason,
            Status = MaintenanceStatus.Scheduled,
            CreatedByUserId = _currentUser.UserId?.ToString() ?? string.Empty,
        };

        _db.MaintenanceWindows.Add(window);

        _db.AuditLogEntries.Add(new AuditLogEntry
        {
            ActorEmail = _currentUser.Email ?? "unknown",
            Action = "Scheduled maintenance window",
            Target = $"{request.StartsAtUtc:u} - {request.EndsAtUtc:u}",
            Level = "Info",
        });

        await _db.SaveChangesAsync(ct);

        return Ok(new { window.Id });
    }

    [HttpPost("maintenance/{id:guid}/cancel")]
    public async Task<ActionResult> CancelMaintenanceWindow(Guid id, CancellationToken ct)
    {
        var window = await _db.MaintenanceWindows.FirstOrDefaultAsync(w => w.Id == id, ct);
        if (window is null) return NotFound();

        window.Status = MaintenanceStatus.Cancelled;
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpGet("logs")]
    public async Task<ActionResult> GetAuditLogs([FromQuery] string? search, [FromQuery] string? level,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.AuditLogEntries.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(l => l.ActorEmail.Contains(search) || l.Action.Contains(search) || l.Target.Contains(search));
        if (!string.IsNullOrWhiteSpace(level)) query = query.Where(l => l.Level == level);
        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(l => l.CreatedAtUtc).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(new { items, page, pageSize, totalCount, totalPages = (int)Math.Ceiling(totalCount / (double)pageSize) });
    }

    [HttpGet("payments")]
    public async Task<ActionResult> GetPayments(CancellationToken ct)
    {
        var payments = await _db.PaymentTransactions
            .Include(p => p.User)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(200)
            .Select(p => new
            {
                p.Id,
                UserEmail = p.User != null ? p.User.Email : null,
                Plan = p.Plan.ToString(),
                p.AmountZar,
                p.Status,
                p.CreatedAtUtc,
            })
            .ToListAsync(ct);

        return Ok(payments);
    }

    [HttpGet("payment-summary")]
    public async Task<ActionResult> GetPaymentSummary(CancellationToken ct)
    {
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var paid = _db.PaymentTransactions.AsNoTracking().Where(p => p.Status == "Paid");
        return Ok(new
        {
            MonthlyRevenue = await paid.Where(p => p.CreatedAtUtc >= monthStart).SumAsync(p => p.AmountZar, ct),
            ActiveSubscriptions = await _db.Users.CountAsync(u => u.Plan != SubscriptionPlan.Free && !u.IsSuspended, ct),
            MonthlyPlans = await _db.Users.CountAsync(u => u.Plan == SubscriptionPlan.PremiumMonthly && !u.IsSuspended, ct),
            YearlyPlans = await _db.Users.CountAsync(u => u.Plan == SubscriptionPlan.PremiumYearly && !u.IsSuspended, ct)
        });
    }

    [HttpGet("analytics")]
    public async Task<ActionResult> GetAnalytics(CancellationToken ct)
    {
        var completed = _db.ExamAttempts.AsNoTracking().Where(a => a.CompletedAtUtc != null);
        var accuracy = await completed.Select(a => (double?)a.Score).AverageAsync(ct) ?? 0;
        var duration = await completed.Where(a => a.Mode == ExamMode.MockExam).Select(a => (double?)a.DurationSeconds).AverageAsync(ct) ?? 0;
        var failedTopic = await _db.ExamAnswers.AsNoTracking().Where(a => !a.IsCorrect && a.Question != null)
            .GroupBy(a => a.Question!.Topic).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefaultAsync(ct);
        return Ok(new { QuestionAccuracyPercent = Math.Round(accuracy, 1), AverageMockExamDurationMinutes = Math.Round(duration / 60, 1), MostFailedTopic = failedTopic ?? "No data" });
    }

    public record UpdateExamRulesRequest(int ExamDurationMinutes, int PassingScorePercent, int MockExamQuestionCount);

    [HttpPut("certifications/{id:guid}/exam-rules")]
    public async Task<ActionResult> UpdateExamRules(Guid id, UpdateExamRulesRequest request, CancellationToken ct)
    {
        if (request.ExamDurationMinutes is < 1 or > 480 || request.PassingScorePercent is < 1 or > 100 || request.MockExamQuestionCount is < 1 or > 500)
            return BadRequest(new { error = "Duration must be 1-480 minutes, pass mark 1-100, and question count 1-500." });
        var certification = await _db.Certifications.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (certification is null) return NotFound();
        certification.ExamDurationMinutes = request.ExamDurationMinutes;
        certification.PassingScorePercent = request.PassingScorePercent;
        certification.MockExamQuestionCount = request.MockExamQuestionCount;
        _db.AuditLogEntries.Add(new AuditLogEntry { ActorEmail = _currentUser.Email ?? "unknown", Action = "Updated certification exam rules", Target = certification.Code, Level = "Info" });
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
