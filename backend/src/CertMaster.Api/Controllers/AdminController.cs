using CertMaster.Application.Common.Interfaces;
using CertMaster.Application.Features.Admin;
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

    public AdminController(AdminService adminService, IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _adminService = adminService;
        _db = db;
        _currentUser = currentUser;
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
