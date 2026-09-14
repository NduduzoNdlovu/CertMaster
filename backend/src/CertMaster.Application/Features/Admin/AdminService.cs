using CertMaster.Application.Common.Interfaces;
using CertMaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CertMaster.Application.Features.Admin;

public record RegistrationByDayDto(string Date, int Count);

public record SystemStatusDto(
    string Database,
    string Api,
    string BackgroundJobs);

public record AdminOverviewDto(
    int TotalUsers,
    int PremiumUsers,
    int ActiveUsersToday,
    int UsersOnlineNow,
    int UsersTakingExamsNow,
    int PracticeSessionsRunning,
    decimal MonthlyRevenue,
    decimal MonthlyGrowthPercent,
    string MostPopularCertification,
    List<RegistrationByDayDto> RegistrationsByDay,
    SystemStatusDto SystemStatus);

public class AdminService
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public AdminService(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<AdminOverviewDto> GetOverviewAsync(CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var today = now.Date;
        var onlineCutoff = now.AddMinutes(-5);
        var currentPeriodStart = now.AddDays(-30);
        var previousPeriodStart = now.AddDays(-60);
        var registrationStart = today.AddDays(-6);

        var totalUsers = await _db.Users.CountAsync(ct);
        var premiumUsers = await _db.Users.CountAsync(u => u.Plan != SubscriptionPlan.Free, ct);
        var activeToday = await _db.Users.CountAsync(u => u.LastActivityAtUtc >= today, ct);
        var usersOnlineNow = await _db.Users.CountAsync(u => u.LastActivityAtUtc >= onlineCutoff, ct);

        var usersTakingExamsNow = await _db.ExamAttempts
            .Where(a => a.CompletedAtUtc == null)
            .Select(a => a.UserId)
            .Distinct()
            .CountAsync(ct);

        var practiceSessionsRunning = await _db.ExamAttempts.CountAsync(
            a => a.CompletedAtUtc == null && a.Mode == ExamMode.Practice,
            ct);

        var monthlyRevenue = await _db.PaymentTransactions
            .Where(p => p.Status == "Paid" && p.CreatedAtUtc >= currentPeriodStart)
            .SumAsync(p => (decimal?)p.AmountZar, ct) ?? 0;

        var previousMonthlyRevenue = await _db.PaymentTransactions
            .Where(p => p.Status == "Paid" &&
                        p.CreatedAtUtc >= previousPeriodStart &&
                        p.CreatedAtUtc < currentPeriodStart)
            .SumAsync(p => (decimal?)p.AmountZar, ct) ?? 0;

        var monthlyGrowthPercent = previousMonthlyRevenue == 0
            ? monthlyRevenue > 0 ? 100m : 0m
            : Math.Round(
                (monthlyRevenue - previousMonthlyRevenue) / previousMonthlyRevenue * 100m,
                1);

        var mostPopular = await _db.ExamAttempts
            .GroupBy(a => a.CertificationId)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefaultAsync(ct);

        var mostPopularName = mostPopular == Guid.Empty
            ? "N/A"
            : (await _db.Certifications.FirstOrDefaultAsync(c => c.Id == mostPopular, ct))?.Name ?? "N/A";

        var registrationCounts = await _db.Users
            .Where(u => u.CreatedAtUtc >= registrationStart)
            .GroupBy(u => u.CreatedAtUtc.Date)
            .Select(group => new { Date = group.Key, Count = group.Count() })
            .ToListAsync(ct);

        var registrationsByDay = Enumerable.Range(0, 7)
            .Select(offset => registrationStart.AddDays(offset))
            .Select(date => new RegistrationByDayDto(
                date.ToString("yyyy-MM-dd"),
                registrationCounts.FirstOrDefault(item => item.Date == date)?.Count ?? 0))
            .ToList();

        // Reaching this point means both the API request and its database queries
        // succeeded. Imports currently run synchronously, so there is no background
        // worker to report as running yet.
        var systemStatus = new SystemStatusDto(
            Database: "Online",
            Api: "Online",
            BackgroundJobs: "Paused");

        return new AdminOverviewDto(
            totalUsers,
            premiumUsers,
            activeToday,
            usersOnlineNow,
            usersTakingExamsNow,
            practiceSessionsRunning,
            monthlyRevenue,
            monthlyGrowthPercent,
            mostPopularName,
            registrationsByDay,
            systemStatus);
    }
}
