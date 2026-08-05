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
    public async Task<ActionResult> GetUsers([FromQuery] string? search, CancellationToken ct)
    {
        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(u => u.FullName.ToLower().Contains(term) || u.Email.ToLower().Contains(term));
        }

        var users = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Take(200)
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

        return Ok(users);
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
    public async Task<ActionResult> GetAuditLogs(CancellationToken ct)
    {
        var logs = await _db.AuditLogEntries
            .OrderByDescending(l => l.CreatedAtUtc)
            .Take(200)
            .ToListAsync(ct);

        return Ok(logs);
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
}
