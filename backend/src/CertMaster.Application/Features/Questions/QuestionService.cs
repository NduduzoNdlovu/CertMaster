using CertMaster.Application.Common.Interfaces;
using CertMaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CertMaster.Application.Features.Questions;

public record QuestionOptionDto(Guid Id, string Text, bool IsCorrect);

public record QuestionDto(
    Guid Id,
    Guid CertificationId,
    string Topic,
    string? Subtopic,
    string Difficulty,
    string Prompt,
    string Explanation,
    string? Reference,
    string? ImageUrl,
    List<QuestionOptionDto> Options);

public class QuestionService
{
    private readonly IApplicationDbContext _db;

    public QuestionService(IApplicationDbContext db) => _db = db;

    public async Task<List<QuestionDto>> GetPracticeQuestionsAsync(
        Guid? certificationId, string? topic, Difficulty? difficulty, int take, CancellationToken ct)
    {
        var query = _db.Questions
            .Where(q => q.Status == QuestionStatus.Published);

        if (certificationId is not null)
            query = query.Where(q => q.CertificationId == certificationId);

        if (!string.IsNullOrWhiteSpace(topic))
            query = query.Where(q => q.Topic == topic);

        if (difficulty is not null)
            query = query.Where(q => q.Difficulty == difficulty);

        return await query
            .OrderBy(q => Guid.NewGuid()) // shuffle server-side
            .Take(take)
            .Select(q => new QuestionDto(
                q.Id,
                q.CertificationId,
                q.Topic,
                q.Subtopic,
                q.Difficulty.ToString(),
                q.Prompt,
                q.Explanation,
                q.Reference,
                q.ImageUrl,
                q.Options.OrderBy(o => o.SortOrder)
                    .Select(o => new QuestionOptionDto(o.Id, o.Text, o.IsCorrect))
                    .ToList()))
            .ToListAsync(ct);
    }
}
