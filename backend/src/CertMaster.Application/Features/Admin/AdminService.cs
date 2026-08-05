using CertMaster.Application.Common.Interfaces;
using CertMaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CertMaster.Application.Features.Admin;

public record AdminOverviewDto(
    int TotalUsers,
    int PremiumUsers,
    int ActiveUsersToday,
    int MonthlyRevenue,
    string MostPopularCertification);

public class AdminService
{
    private readonly IApplicationDbContext _db;

    public AdminService(IApplicationDbContext db) => _db = db;

    public async Task<AdminOverviewDto> GetOverviewAsync(CancellationToken ct)
    {
        var totalUsers = await _db.Users.CountAsync(ct);
        var premiumUsers = await _db.Users.CountAsync(u => u.Plan != SubscriptionPlan.Free, ct);
        var activeToday = await _db.Users.CountAsync(u => u.LastActivityAtUtc >= DateTime.UtcNow.Date, ct);
        var monthlyRevenue = await _db.PaymentTransactions
            .Where(p => p.Status == "Paid" && p.CreatedAtUtc >= DateTime.UtcNow.AddDays(-30))
            .SumAsync(p => (decimal?)p.AmountZar, ct) ?? 0;

        var mostPopular = await _db.ExamAttempts
            .GroupBy(a => a.CertificationId)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefaultAsync(ct);

        var mostPopularName = mostPopular == Guid.Empty
            ? "N/A"
            : (await _db.Certifications.FirstOrDefaultAsync(c => c.Id == mostPopular, ct))?.Name ?? "N/A";

        return new AdminOverviewDto(totalUsers, premiumUsers, activeToday, (int)monthlyRevenue, mostPopularName);
    }
}
