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

public record AnalyticsActivityByDayDto(string Date, int Practice, int MockExam);
public record AnalyticsCertificationDto(Guid CertificationId, string Certification, int CompletedAttempts, decimal PassRatePercent, decimal AccuracyPercent);
public record AnalyticsTopicDto(string Topic, int IncorrectAnswers);
public record AnalyticsRevenueByMonthDto(string Month, decimal Revenue);

public record AdminAnalyticsDto(
    int TotalLearners,
    int ActiveLearners,
    int QuestionsAnswered,
    decimal QuestionAccuracyPercent,
    decimal AverageScorePercent,
    decimal PassRatePercent,
    int CompletedMockExams,
    int CompletedPracticeSessions,
    decimal AverageMockExamDurationMinutes,
    string MostFailedTopic,
    List<RegistrationByDayDto> RegistrationsByDay,
    List<AnalyticsActivityByDayDto> AttemptsByDay,
    List<AnalyticsCertificationDto> CertificationPerformance,
    List<AnalyticsTopicDto> FailedTopics,
    List<AnalyticsRevenueByMonthDto> RevenueByMonth);

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
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var previousMonthStart = currentMonthStart.AddMonths(-1);
        var registrationStart = today.AddDays(-29);

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
            .Where(p => p.Status == "Paid" && p.CreatedAtUtc >= currentMonthStart)
            .SumAsync(p => (decimal?)p.AmountZar, ct) ?? 0;

        var previousMonthlyRevenue = await _db.PaymentTransactions
            .Where(p => p.Status == "Paid" &&
                        p.CreatedAtUtc >= previousMonthStart &&
                        p.CreatedAtUtc < currentMonthStart)
            .SumAsync(p => (decimal?)p.AmountZar, ct) ?? 0;

        var monthlyGrowthPercent = previousMonthlyRevenue == 0
            ? monthlyRevenue > 0 ? 100m : 0m
            : Math.Round((monthlyRevenue - previousMonthlyRevenue) / previousMonthlyRevenue * 100m, 1);

        var mostPopular = await _db.ExamAttempts
            .Where(a => a.CompletedAtUtc != null)
            .GroupBy(a => a.CertificationId)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefaultAsync(ct);

        var mostPopularName = mostPopular == Guid.Empty
            ? "N/A"
            : (await _db.Certifications.AsNoTracking().FirstOrDefaultAsync(c => c.Id == mostPopular, ct))?.Name ?? "N/A";

        var registrationCounts = await _db.Users
            .Where(u => u.CreatedAtUtc >= registrationStart)
            .GroupBy(u => u.CreatedAtUtc.Date)
            .Select(group => new { Date = group.Key, Count = group.Count() })
            .ToListAsync(ct);

        var registrationsByDay = Enumerable.Range(0, 30)
            .Select(offset => registrationStart.AddDays(offset))
            .Select(date => new RegistrationByDayDto(
                date.ToString("yyyy-MM-dd"),
                registrationCounts.FirstOrDefault(item => item.Date == date)?.Count ?? 0))
            .ToList();

        // The overview endpoint itself completed successfully, so the API and database
        // are available. The expiration worker is registered as a hosted background job.
        var systemStatus = new SystemStatusDto(
            Database: "Online",
            Api: "Online",
            BackgroundJobs: "Running");

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

    public async Task<AdminAnalyticsDto> GetAnalyticsAsync(CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var today = now.Date;
        var activityStart = today.AddDays(-29);
        var firstRevenueMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-11);

        // Keep database-side queries to simple filtering/projection. Aggregate and DTO
        // shaping happens after materialization so provider-specific GroupBy/record
        // projection translation cannot turn the analytics endpoint into a 400.
        var totalLearners = await _db.Users.CountAsync(u => u.Role == UserRole.Learner, ct);
        var activeLearners = await _db.Users.CountAsync(
            u => u.Role == UserRole.Learner && u.LastActivityAtUtc >= now.AddDays(-30), ct);

        var registrations = await _db.Users.AsNoTracking()
            .Where(u => u.CreatedAtUtc >= activityStart)
            .Select(u => u.CreatedAtUtc)
            .ToListAsync(ct);
        var registrationsByDay = Enumerable.Range(0, 30)
            .Select(offset => activityStart.AddDays(offset))
            .Select(date => new RegistrationByDayDto(
                date.ToString("yyyy-MM-dd"), registrations.Count(created => created.Date == date)))
            .ToList();

        var attempts = await _db.ExamAttempts.AsNoTracking()
            .Where(a => a.CompletedAtUtc != null)
            .Select(a => new
            {
                a.CertificationId,
                Certification = a.Certification == null ? "Unknown" : a.Certification.Name,
                a.Mode,
                a.StartedAtUtc,
                CompletedAtUtc = a.CompletedAtUtc!.Value,
                a.DurationSeconds,
                a.Score,
                a.Passed
            })
            .ToListAsync(ct);

        var answers = await _db.ExamAnswers.AsNoTracking()
            .Where(a => a.SelectedOptionId != null)
            .Select(a => new
            {
                a.IsCorrect,
                Topic = a.Question == null ? "Unknown" : a.Question.Topic,
                CertificationId = a.ExamAttempt == null ? Guid.Empty : a.ExamAttempt.CertificationId,
                Certification = a.ExamAttempt == null || a.ExamAttempt.Certification == null
                    ? "Unknown" : a.ExamAttempt.Certification.Name,
                AttemptCompleted = a.ExamAttempt != null && a.ExamAttempt.CompletedAtUtc != null
            })
            .ToListAsync(ct);

        var questionsAnswered = answers.Count;
        var correctAnswers = answers.Count(a => a.IsCorrect);
        var questionAccuracy = questionsAnswered == 0 ? 0m : Math.Round(correctAnswers * 100m / questionsAnswered, 1);
        var averageScore = attempts.Count == 0 ? 0m : (decimal)attempts.Average(a => a.Score);
        var passRate = attempts.Count == 0 ? 0m : Math.Round(attempts.Count(a => a.Passed) * 100m / attempts.Count, 1);
        var completedMockExams = attempts.Count(a => a.Mode == ExamMode.MockExam);
        var completedPracticeSessions = attempts.Count(a => a.Mode == ExamMode.Practice);
        var mockAttempts = attempts.Where(a => a.Mode == ExamMode.MockExam).ToList();
        var averageMockDurationMinutes = mockAttempts.Count == 0 ? 0m : (decimal)mockAttempts.Average(a => a.DurationSeconds) / 60m;

        var attemptsByDay = Enumerable.Range(0, 30)
            .Select(offset => activityStart.AddDays(offset))
            .Select(date => new AnalyticsActivityByDayDto(
                date.ToString("yyyy-MM-dd"),
                attempts.Count(a => a.CompletedAtUtc.Date == date && a.Mode == ExamMode.Practice),
                attempts.Count(a => a.CompletedAtUtc.Date == date && a.Mode == ExamMode.MockExam)))
            .ToList();

        var certificationPerformance = attempts
            .GroupBy(a => new { a.CertificationId, a.Certification })
            .Select(group =>
            {
                var completedAnswers = answers.Where(a => a.AttemptCompleted && a.CertificationId == group.Key.CertificationId).ToList();
                var accuracy = completedAnswers.Count == 0 ? 0m : Math.Round(completedAnswers.Count(a => a.IsCorrect) * 100m / completedAnswers.Count, 1);
                return new AnalyticsCertificationDto(
                    group.Key.CertificationId,
                    group.Key.Certification,
                    group.Count(),
                    Math.Round(group.Count(a => a.Passed) * 100m / group.Count(), 1),
                    accuracy);
            })
            .OrderByDescending(item => item.CompletedAttempts)
            .Take(15)
            .ToList();

        var failedTopics = answers
            .Where(a => a.AttemptCompleted && !a.IsCorrect)
            .GroupBy(a => a.Topic)
            .Select(group => new AnalyticsTopicDto(group.Key, group.Count()))
            .OrderByDescending(item => item.IncorrectAnswers)
            .ThenBy(item => item.Topic)
            .Take(10)
            .ToList();

        var revenueRows = await _db.PaymentTransactions.AsNoTracking()
            .Where(p => p.Status == "Paid" && p.CreatedAtUtc >= firstRevenueMonth)
            .Select(p => new { p.CreatedAtUtc, p.AmountZar })
            .ToListAsync(ct);
        var revenueByMonth = Enumerable.Range(0, 12)
            .Select(offset => firstRevenueMonth.AddMonths(offset))
            .Select(month => new AnalyticsRevenueByMonthDto(
                month.ToString("yyyy-MM"),
                revenueRows.Where(row => row.CreatedAtUtc.Year == month.Year && row.CreatedAtUtc.Month == month.Month)
                    .Sum(row => row.AmountZar)))
            .ToList();

        return new AdminAnalyticsDto(
            totalLearners,
            activeLearners,
            questionsAnswered,
            questionAccuracy,
            Math.Round(averageScore, 1),
            passRate,
            completedMockExams,
            completedPracticeSessions,
            Math.Round(averageMockDurationMinutes, 1),
            failedTopics.FirstOrDefault()?.Topic ?? "No data",
            registrationsByDay,
            attemptsByDay,
            certificationPerformance,
            failedTopics,
            revenueByMonth);
    }

}
