using CertMaster.Domain.Enums;
using CertMaster.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CertMaster.Api.Services;

public sealed class ExpiredExamSubmissionWorker(IServiceScopeFactory scopeFactory, ILogger<ExpiredExamSubmissionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var candidates = await db.ExamAttempts.Include(a => a.Certification)
                    .Include(a => a.Answers).ThenInclude(a => a.Question).ThenInclude(q => q!.Options)
                    .Where(a => a.CompletedAtUtc == null).ToListAsync(stoppingToken);
                var now = DateTime.UtcNow;
                foreach (var attempt in candidates.Where(a => now >= a.StartedAtUtc.AddMinutes(a.Certification!.ExamDurationMinutes)))
                {
                    foreach (var answer in attempt.Answers)
                        answer.IsCorrect = answer.Question?.Options.Any(o => o.Id == answer.SelectedOptionId && o.IsCorrect) == true;
                    attempt.CorrectCount = attempt.Answers.Count(a => a.IsCorrect); attempt.TotalQuestions = attempt.Answers.Count;
                    attempt.Score = attempt.TotalQuestions == 0 ? 0 : (int)Math.Round(attempt.CorrectCount * 100.0 / attempt.TotalQuestions);
                    attempt.Passed = attempt.Score >= attempt.Certification!.PassingScorePercent;
                    attempt.CompletedAtUtc = now; attempt.DurationSeconds = Math.Max(0, (int)(now - attempt.StartedAtUtc).TotalSeconds);
                }
                if (db.ChangeTracker.HasChanges()) await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex) { logger.LogError(ex, "Failed to automatically submit expired exams."); }
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
