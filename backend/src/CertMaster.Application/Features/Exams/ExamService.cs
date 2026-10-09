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
public record ExamAttemptSummaryDto(Guid Id, Guid CertificationId, string CertificationName, string Mode,
    DateTime StartedAt, DateTime? CompletedAt, int DurationSeconds, int TotalQuestions, int CorrectCount, int Score, bool Passed);
public record ExamAnswerResultDto(Guid QuestionId, string Prompt, Guid? SelectedOptionId, string? SelectedOption,
    Guid? CorrectOptionId, string? CorrectOption, bool IsCorrect, bool WasFlaggedForReview, string Explanation);
public record ExamAttemptDetailDto(ExamAttemptSummaryDto Attempt, IReadOnlyList<ExamAnswerResultDto> Answers);
public record StartExamRequest(Guid CertificationId, string Mode);
public record SaveExamAnswerRequest(Guid? SelectedOptionId, bool WasFlaggedForReview);
public record PracticeAnswerResultDto(Guid QuestionId, bool IsCorrect, Guid? CorrectOptionId, string? CorrectOption, string Explanation);
public record ExamQuestionOptionDto(Guid Id, string Text);
public record ExamSessionQuestionDto(Guid Id, string Topic, string Prompt, IReadOnlyList<ExamQuestionOptionDto> Options,
    Guid? SelectedOptionId, bool WasFlaggedForReview);
public record ExamSessionDto(Guid AttemptId, Guid CertificationId, string CertificationName, string Mode,
    DateTime StartedAtUtc, DateTime ExpiresAtUtc, DateTime ServerTimeUtc, int PassingScorePercent,
    IReadOnlyList<ExamSessionQuestionDto> Questions);

public class ExamService
{
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

        var certification = await _db.Certifications.FirstOrDefaultAsync(c => c.Id == request.CertificationId, ct)
            ?? throw new KeyNotFoundException("Certification not found.");
        var questionIds = request.Answers.Select(a => a.QuestionId).Distinct().ToList();
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
        attempt.Passed = attempt.Score >= certification.PassingScorePercent;

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

    public async Task<ExamSessionDto> StartOrResumeAsync(StartExamRequest request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        if (!Enum.TryParse<ExamMode>(request.Mode, true, out var mode)) throw new ArgumentException("Invalid exam mode.");
        var existing = await LoadActiveAttemptAsync(userId, request.CertificationId, mode, ct);
        if (existing is not null)
        {
            if (HasExpired(existing)) await CompleteAttemptAsync(existing, ct);
            else return ToSession(existing);
        }

        var certification = await _db.Certifications.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.CertificationId && c.IsActive, ct)
            ?? throw new KeyNotFoundException("Certification not found.");
        var take = mode == ExamMode.MockExam ? certification.MockExamQuestionCount : Math.Min(20, certification.MockExamQuestionCount);
        var questions = await _db.Questions.Include(q => q.Options)
            .Where(q => q.CertificationId == certification.Id && q.Status == QuestionStatus.Published)
            .OrderBy(_ => Guid.NewGuid()).Take(take).ToListAsync(ct);
        if (questions.Count == 0) throw new InvalidOperationException("No published questions are available.");

        // var attempt = new ExamAttempt { UserId = userId, CertificationId = certification.Id, Certification = certification,
        //     Mode = mode, StartedAtUtc = DateTime.UtcNow, TotalQuestions = questions.Count };

var attempt = new ExamAttempt
{
    UserId = userId,
    CertificationId = certification.Id,
    Mode = mode,
    StartedAtUtc = DateTime.UtcNow,
    TotalQuestions = questions.Count
};


        foreach (var question in questions) attempt.Answers.Add(new ExamAnswer { QuestionId = question.Id, Question = question });
        _db.ExamAttempts.Add(attempt);
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateException)
        {
            _db.ExamAttempts.Remove(attempt);
            var concurrent = await LoadActiveAttemptAsync(userId, request.CertificationId, mode, ct);
            if (concurrent is null) throw;
            return ToSession(concurrent);
        }
        return ToSession(attempt);
    }

    public async Task<ExamSessionDto?> GetActiveAsync(Guid certificationId, string modeValue, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        if (!Enum.TryParse<ExamMode>(modeValue, true, out var mode)) return null;
        var attempt = await LoadActiveAttemptAsync(userId, certificationId, mode, ct);
        if (attempt is null) return null;
        if (HasExpired(attempt)) { await CompleteAttemptAsync(attempt, ct); return null; }
        return ToSession(attempt);
    }

    public async Task SaveAnswerAsync(Guid attemptId, Guid questionId, SaveExamAnswerRequest request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        var attempt = await LoadAttemptAsync(attemptId, userId, ct) ?? throw new KeyNotFoundException("Active exam not found.");
        if (attempt.CompletedAtUtc is not null) throw new InvalidOperationException("This exam is already complete.");
        if (HasExpired(attempt)) { await CompleteAttemptAsync(attempt, ct); throw new InvalidOperationException("Exam time has expired."); }
        var answer = attempt.Answers.FirstOrDefault(a => a.QuestionId == questionId) ?? throw new KeyNotFoundException("Question is not part of this exam.");
        if (request.SelectedOptionId is not null && answer.Question?.Options.All(o => o.Id != request.SelectedOptionId) != false)
            throw new ArgumentException("Selected option is invalid.");
        answer.SelectedOptionId = request.SelectedOptionId;
        answer.WasFlaggedForReview = request.WasFlaggedForReview;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PracticeAnswerResultDto> SubmitPracticeAnswerAsync(
        Guid attemptId,
        Guid questionId,
        SaveExamAnswerRequest request,
        CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        var attempt = await LoadAttemptAsync(attemptId, userId, ct)
            ?? throw new KeyNotFoundException("Practice session not found.");

        if (attempt.Mode != ExamMode.Practice)
            throw new InvalidOperationException("This attempt is not a practice session.");

        if (attempt.CompletedAtUtc is not null)
            throw new InvalidOperationException("This practice session is already complete.");

        if (HasExpired(attempt))
        {
            await CompleteAttemptAsync(attempt, ct);
            throw new InvalidOperationException("Practice session has expired.");
        }

        var answer = attempt.Answers.FirstOrDefault(a => a.QuestionId == questionId)
            ?? throw new KeyNotFoundException("Question is not part of this practice session.");

        if (request.SelectedOptionId is not null &&
            answer.Question?.Options.All(o => o.Id != request.SelectedOptionId) != false)
            throw new ArgumentException("Selected option is invalid.");

        answer.SelectedOptionId = request.SelectedOptionId;
        answer.WasFlaggedForReview = request.WasFlaggedForReview;
        answer.IsCorrect = answer.Question?.Options.Any(o => o.Id == request.SelectedOptionId && o.IsCorrect) == true;

        var correct = answer.Question?.Options.FirstOrDefault(o => o.IsCorrect);

        await _db.SaveChangesAsync(ct);

        return new PracticeAnswerResultDto(
            questionId,
            answer.IsCorrect,
            correct?.Id,
            correct?.Text,
            answer.Question?.Explanation ?? string.Empty);
    }

    public async Task<ExamResultDto> CompleteAsync(Guid attemptId, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        var attempt = await LoadAttemptAsync(attemptId, userId, ct) ?? throw new KeyNotFoundException("Exam not found.");
        if (attempt.CompletedAtUtc is null) await CompleteAttemptAsync(attempt, ct);
        return new ExamResultDto(attempt.Id, attempt.TotalQuestions, attempt.CorrectCount, attempt.Score, attempt.Passed);
    }

    private async Task<ExamAttempt?> LoadActiveAttemptAsync(Guid userId, Guid certificationId, ExamMode mode, CancellationToken ct) =>
        await _db.ExamAttempts.Include(a => a.Certification).Include(a => a.Answers).ThenInclude(a => a.Question).ThenInclude(q => q!.Options)
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Mode == mode && a.CompletedAtUtc == null &&
                (mode == ExamMode.MockExam || a.CertificationId == certificationId), ct);

    private async Task<ExamAttempt?> LoadAttemptAsync(Guid attemptId, Guid userId, CancellationToken ct) =>
        await _db.ExamAttempts.Include(a => a.Certification).Include(a => a.Answers).ThenInclude(a => a.Question).ThenInclude(q => q!.Options)
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId, ct);

    private static bool HasExpired(ExamAttempt attempt) => DateTime.UtcNow >= attempt.StartedAtUtc.AddMinutes(attempt.Certification?.ExamDurationMinutes ?? 90);

    private async Task CompleteAttemptAsync(ExamAttempt attempt, CancellationToken ct)
    {
        foreach (var answer in attempt.Answers)
            answer.IsCorrect = answer.Question?.Options.Any(o => o.Id == answer.SelectedOptionId && o.IsCorrect) == true;
        attempt.CorrectCount = attempt.Answers.Count(a => a.IsCorrect);
        attempt.TotalQuestions = attempt.Answers.Count;
        attempt.Score = attempt.TotalQuestions == 0 ? 0 : (int)Math.Round(attempt.CorrectCount * 100.0 / attempt.TotalQuestions);
        attempt.Passed = attempt.Score >= (attempt.Certification?.PassingScorePercent ?? 65);
        attempt.CompletedAtUtc = DateTime.UtcNow;
        attempt.DurationSeconds = Math.Max(0, (int)(attempt.CompletedAtUtc.Value - attempt.StartedAtUtc).TotalSeconds);
        await UpdateStudyStreakAsync(attempt.UserId, ct);
        await _db.SaveChangesAsync(ct);
    }

    private static ExamSessionDto ToSession(ExamAttempt attempt)
    {
        var certification = attempt.Certification ?? throw new InvalidOperationException("Certification was not loaded.");
        return new ExamSessionDto(attempt.Id, attempt.CertificationId, certification.Name, attempt.Mode.ToString(), attempt.StartedAtUtc,
            attempt.StartedAtUtc.AddMinutes(certification.ExamDurationMinutes), DateTime.UtcNow, certification.PassingScorePercent,
            attempt.Answers.OrderBy(a => a.CreatedAtUtc).Select(a => new ExamSessionQuestionDto(a.QuestionId,
                a.Question?.Topic ?? "", a.Question?.Prompt ?? "Question unavailable",
                a.Question?.Options.OrderBy(o => o.SortOrder).Select(o => new ExamQuestionOptionDto(o.Id, o.Text)).ToList() ?? [],
                a.SelectedOptionId, a.WasFlaggedForReview)).ToList());
    }

    public async Task<List<ExamAttemptSummaryDto>> GetMyResultsAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        return await _db.ExamAttempts.AsNoTracking()
            .Where(a => a.UserId == userId && a.CompletedAtUtc != null)
            .OrderByDescending(a => a.CompletedAtUtc)
            .Select(a => new ExamAttemptSummaryDto(a.Id, a.CertificationId,
                a.Certification != null ? a.Certification.Name : "Unknown", a.Mode.ToString(), a.StartedAtUtc,
                a.CompletedAtUtc, a.DurationSeconds, a.TotalQuestions, a.CorrectCount, a.Score, a.Passed))
            .ToListAsync(ct);
    }

    public async Task<ExamAttemptDetailDto?> GetMyResultAsync(Guid attemptId, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

        var attempt = await _db.ExamAttempts.AsNoTracking()
            .Include(a => a.Certification).Include(a => a.Answers).ThenInclude(a => a.Question).ThenInclude(q => q!.Options)
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId && a.CompletedAtUtc != null, ct);
        if (attempt is null) return null;
        var summary = new ExamAttemptSummaryDto(attempt.Id, attempt.CertificationId, attempt.Certification?.Name ?? "Unknown",
            attempt.Mode.ToString(), attempt.StartedAtUtc, attempt.CompletedAtUtc, attempt.DurationSeconds,
            attempt.TotalQuestions, attempt.CorrectCount, attempt.Score, attempt.Passed);
        var answers = attempt.Answers.OrderBy(a => a.CreatedAtUtc).Select(a =>
        {
            var selected = a.Question?.Options.FirstOrDefault(o => o.Id == a.SelectedOptionId);
            var correct = a.Question?.Options.FirstOrDefault(o => o.IsCorrect);
            return new ExamAnswerResultDto(a.QuestionId, a.Question?.Prompt ?? "Question unavailable", a.SelectedOptionId,
                selected?.Text, correct?.Id, correct?.Text, a.IsCorrect, a.WasFlaggedForReview, a.Question?.Explanation ?? "");
        }).ToList();
        return new ExamAttemptDetailDto(summary, answers);
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
