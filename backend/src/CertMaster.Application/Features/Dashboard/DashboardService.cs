using CertMaster.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CertMaster.Application.Features.Dashboard;

public record TopicMasteryDto(string Topic, int MasteryPercent);

public record RecentAttemptDto(Guid Id, string Mode, DateTime StartedAtUtc, int Score, bool Passed, int TotalQuestions, int CorrectCount);

public record DashboardSummaryDto(
    int StudyStreakDays,
    int QuestionsAnswered,
    int AverageScore,
    int PassRate,
    int StudyTimeMinutesThisWeek,
    int ExamReadinessScore,
    List<TopicMasteryDto> WeakTopics,
    List<TopicMasteryDto> StrongTopics,
    List<RecentAttemptDto> RecentAttempts);

public class DashboardService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DashboardService(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            throw new UnauthorizedAccessException();

        var userId = _currentUser.UserId.Value;

        var attempts = await _db.ExamAttempts
            .Where(a => a.UserId == userId && a.CompletedAtUtc != null)
            .OrderByDescending(a => a.StartedAtUtc)
            .ToListAsync(ct);

        var user = await _db.Users.FirstAsync(u => u.Id == userId, ct);

        var answeredCount = await _db.ExamAnswers
            .CountAsync(a => a.ExamAttempt!.UserId == userId, ct);

        var averageScore = attempts.Count == 0 ? 0 : (int)attempts.Average(a => a.Score);
        var passRate = attempts.Count == 0 ? 0 : (int)Math.Round(attempts.Count(a => a.Passed) * 100.0 / attempts.Count);

        // Topic mastery derived from answer correctness per topic
        var topicStats = await _db.ExamAnswers
            .Where(a => a.ExamAttempt!.UserId == userId)
            .GroupBy(a => a.Question!.Topic)
            .Select(g => new { Topic = g.Key, Correct = g.Count(a => a.IsCorrect), Total = g.Count() })
            .ToListAsync(ct);

        var masteryByTopic = topicStats
            .Select(t => new TopicMasteryDto(t.Topic, t.Total == 0 ? 0 : (int)Math.Round(t.Correct * 100.0 / t.Total)))
            .ToList();

        var totalCertificationTopics = await _db.Topics
            .CountAsync(t => attempts.Select(a => a.CertificationId).Distinct().Contains(t.CertificationId), ct);

        var readinessScore = CalculateReadinessScore(attempts, masteryByTopic, totalCertificationTopics);

        return new DashboardSummaryDto(
            StudyStreakDays: user.StudyStreakDays,
            QuestionsAnswered: answeredCount,
            AverageScore: averageScore,
            PassRate: passRate,
            StudyTimeMinutesThisWeek: attempts
                .Where(a => a.StartedAtUtc >= DateTime.UtcNow.AddDays(-7))
                .Sum(a => a.DurationSeconds) / 60,
            ExamReadinessScore: readinessScore,
            WeakTopics: masteryByTopic.OrderBy(t => t.MasteryPercent).Take(3).ToList(),
            StrongTopics: masteryByTopic.OrderByDescending(t => t.MasteryPercent).Take(3).ToList(),
            RecentAttempts: attempts.Take(5)
                .Select(a => new RecentAttemptDto(a.Id, a.Mode.ToString(), a.StartedAtUtc, a.Score, a.Passed, a.TotalQuestions, a.CorrectCount))
                .ToList());
    }

    /// <summary>
    /// A simple, explainable heuristic: 50% recent performance (last 5 attempts),
    /// 30% overall pass rate, 20% topic breadth (how many of the certification's
    /// topics have been practiced at all). This is not adaptive/ML-based — it's a
    /// transparent starting formula that can be refined with real usage data later.
    /// </summary>
    private static int CalculateReadinessScore(
        List<Domain.Entities.ExamAttempt> attempts, List<TopicMasteryDto> masteryByTopic, int totalCertificationTopics)
    {
        if (attempts.Count == 0) return 0;

        var recentAverage = attempts.Take(5).Average(a => a.Score);
        var passRate = attempts.Count(a => a.Passed) * 100.0 / attempts.Count;
        var breadth = totalCertificationTopics == 0
            ? 100.0
            : Math.Min(100.0, masteryByTopic.Count * 100.0 / totalCertificationTopics);

        var score = (recentAverage * 0.5) + (passRate * 0.3) + (breadth * 0.2);
        return (int)Math.Round(Math.Clamp(score, 0, 100));
    }
}
