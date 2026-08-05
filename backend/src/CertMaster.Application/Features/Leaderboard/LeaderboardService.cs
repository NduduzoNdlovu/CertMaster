using CertMaster.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CertMaster.Application.Features.Leaderboard;

public record LeaderboardEntryDto(int Rank, string FullName, int Points, bool IsPremium, bool IsCurrentUser);

public enum LeaderboardPeriod
{
    AllTime,
    Monthly,
}

public class LeaderboardService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public LeaderboardService(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    /// <summary>
    /// Points = 10 per correct answer + a 50 point bonus for each passed mock exam.
    /// Simple and transparent; easy to tune later without changing the shape of the data.
    /// </summary>
    public async Task<List<LeaderboardEntryDto>> GetTopLearnersAsync(int take, LeaderboardPeriod period, CancellationToken ct)
    {
        var scored = await ScoredAttemptsQuery(period)
            .OrderByDescending(x => x.Points)
            .Take(take)
            .ToListAsync(ct);

        var userIds = scored.Select(s => s.UserId).ToList();
        var users = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName, u.Plan })
            .ToListAsync(ct);

        var result = new List<LeaderboardEntryDto>();
        var rank = 1;
        foreach (var s in scored)
        {
            var user = users.FirstOrDefault(u => u.Id == s.UserId);
            if (user is null) continue;

            result.Add(new LeaderboardEntryDto(
                rank,
                user.FullName,
                s.Points,
                user.Plan != Domain.Enums.SubscriptionPlan.Free,
                user.Id == _currentUser.UserId));
            rank++;
        }

        return result;
    }

    /// <summary>Returns the current user's rank even if they're outside the top N, or null if they have no attempts yet.</summary>
    public async Task<LeaderboardEntryDto?> GetMyRankAsync(LeaderboardPeriod period, CancellationToken ct)
    {
        if (_currentUser.UserId is null) return null;

        var allScored = await ScoredAttemptsQuery(period)
            .OrderByDescending(x => x.Points)
            .ToListAsync(ct);

        var index = allScored.FindIndex(x => x.UserId == _currentUser.UserId);
        if (index == -1) return null;

        var user = await _db.Users.FirstAsync(u => u.Id == _currentUser.UserId, ct);
        return new LeaderboardEntryDto(index + 1, user.FullName, allScored[index].Points, user.Plan != Domain.Enums.SubscriptionPlan.Free, true);
    }

    private IQueryable<ScoredUser> ScoredAttemptsQuery(LeaderboardPeriod period)
    {
        var query = _db.ExamAttempts.AsQueryable();

        if (period == LeaderboardPeriod.Monthly)
        {
            var startOfMonth = new DateTime(_clock.UtcNow.Year, _clock.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            query = query.Where(a => a.StartedAtUtc >= startOfMonth);
        }

        return query
            .GroupBy(a => a.UserId)
            .Select(g => new ScoredUser
            {
                UserId = g.Key,
                Points = g.Sum(a => a.CorrectCount) * 10 + g.Count(a => a.Passed && a.Mode == Domain.Enums.ExamMode.MockExam) * 50,
            });
    }

    private class ScoredUser
    {
        public Guid UserId { get; set; }
        public int Points { get; set; }
    }
}
