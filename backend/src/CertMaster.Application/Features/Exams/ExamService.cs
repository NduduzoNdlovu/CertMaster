using CertMaster.Application.Common.Interfaces;
using CertMaster.Application.Features.Notifications;
using CertMaster.Domain.Entities;
using CertMaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CertMaster.Application.Features.Exams;

public record SubmitAnswerDto(Guid QuestionId, Guid? SelectedOptionId, bool WasFlaggedForReview);

public record SubmitExamRequest(
    Guid CertificationId,
    string Mode, // "Practice" or "MockExam"
    int DurationSeconds,
    List<SubmitAnswerDto> Answers);

public record ExamResultDto(Guid AttemptId, int TotalQuestions, int CorrectCount, int Score, bool Passed);

public class ExamService
{
    private const int PassingScorePercent = 65;
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ExamService(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ExamResultDto> SubmitAsync(SubmitExamRequest request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            throw new UnauthorizedAccessException();

        var questionIds = request.Answers.Select(a => a.QuestionId).ToList();
        var questions = await _db.Questions
            .Include(q => q.Options)
            .Where(q => questionIds.Contains(q.Id))
            .ToListAsync(ct);

        var attempt = new ExamAttempt
        {
            UserId = _currentUser.UserId.Value,
            CertificationId = request.CertificationId,
            Mode = Enum.Parse<ExamMode>(request.Mode),
            DurationSeconds = request.DurationSeconds,
            CompletedAtUtc = DateTime.UtcNow,
            TotalQuestions = request.Answers.Count,
        };

        var correctCount = 0;
        foreach (var answer in request.Answers)
        {
            var question = questions.FirstOrDefault(q => q.Id == answer.QuestionId);
            var isCorrect = question?.Options.FirstOrDefault(o => o.Id == answer.SelectedOptionId)?.IsCorrect ?? false;
            if (isCorrect) correctCount++;

            attempt.Answers.Add(new ExamAnswer
            {
                QuestionId = answer.QuestionId,
                SelectedOptionId = answer.SelectedOptionId,
                IsCorrect = isCorrect,
                WasFlaggedForReview = answer.WasFlaggedForReview,
            });
        }

        attempt.CorrectCount = correctCount;
        attempt.Score = attempt.TotalQuestions == 0 ? 0 : (int)Math.Round(correctCount * 100.0 / attempt.TotalQuestions);
        attempt.Passed = attempt.Score >= PassingScorePercent;

        _db.ExamAttempts.Add(attempt);

        await UpdateStudyStreakAsync(attempt.UserId, ct);

        if (attempt.Mode == ExamMode.MockExam)
        {
            var resultWord = attempt.Passed ? "passed" : "did not pass";
            _db.Notifications.Add(NotificationService.Build(
                attempt.UserId,
                attempt.Passed ? "Mock exam passed!" : "Mock exam results are in",
                $"You scored {attempt.Score}% and {resultWord} this mock exam ({attempt.CorrectCount}/{attempt.TotalQuestions} correct).",
                "ExamResult"));
        }

        await _db.SaveChangesAsync(ct);

        return new ExamResultDto(attempt.Id, attempt.TotalQuestions, attempt.CorrectCount, attempt.Score, attempt.Passed);
    }

    /// <summary>
    /// Increments the streak if the user's last recorded activity was yesterday (UTC date),
    /// leaves it unchanged if they've already been active today, and resets to 1 if there's
    /// a gap of more than one day. This is the single place streak logic lives, so any future
    /// activity type (not just exam submissions) can call it the same way.
    /// </summary>
    private async Task UpdateStudyStreakAsync(Guid userId, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return;

        var today = DateTime.UtcNow.Date;
        var lastActivityDate = user.LastActivityAtUtc?.Date;

        if (lastActivityDate == today)
        {
            // Already active today — streak unchanged.
        }
        else if (lastActivityDate == today.AddDays(-1))
        {
            user.StudyStreakDays += 1;
        }
        else
        {
            user.StudyStreakDays = 1;
        }

        user.LastActivityAtUtc = DateTime.UtcNow;
    }
}
