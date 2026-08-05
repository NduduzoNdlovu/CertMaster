using CertMaster.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CertMaster.Application.Features.Flashcards;

public record FlashcardDto(Guid Id, string Topic, string Front, string Back, string? Reference);

public class FlashcardService
{
    private readonly IApplicationDbContext _db;

    public FlashcardService(IApplicationDbContext db) => _db = db;

    public async Task<List<FlashcardDto>> GetFlashcardsAsync(Guid? certificationId, string? topic, int take, CancellationToken ct)
    {
        var query = _db.Questions.Where(q => q.Status == Domain.Enums.QuestionStatus.Published);

        if (certificationId is not null)
            query = query.Where(q => q.CertificationId == certificationId);

        if (!string.IsNullOrWhiteSpace(topic))
            query = query.Where(q => q.Topic == topic);

        return await query
            .OrderBy(q => Guid.NewGuid())
            .Take(take)
            .Select(q => new FlashcardDto(
                q.Id,
                q.Topic,
                q.Prompt,
                q.Explanation,
                q.Reference))
            .ToListAsync(ct);
    }
}
