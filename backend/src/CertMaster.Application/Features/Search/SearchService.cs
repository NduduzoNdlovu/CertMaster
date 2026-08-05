using CertMaster.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CertMaster.Application.Features.Search;

public record SearchResultDto(string Type, string Title, string Subtitle, Guid Id, Guid? CertificationId);

public class SearchService
{
    private readonly IApplicationDbContext _db;

    public SearchService(IApplicationDbContext db) => _db = db;

    public async Task<List<SearchResultDto>> SearchAsync(string term, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2)
            return new List<SearchResultDto>();

        var normalized = term.Trim();
        var results = new List<SearchResultDto>();

        var certMatches = await _db.Certifications
            .Where(c => c.IsActive && (c.Name.Contains(normalized) || c.Code.Contains(normalized)))
            .Take(5)
            .Select(c => new SearchResultDto("Certification", c.Name, c.Code, c.Id, c.Id))
            .ToListAsync(ct);
        results.AddRange(certMatches);

        var topicMatches = await _db.Questions
            .Where(q => q.Topic.Contains(normalized))
            .Select(q => new { q.Topic, q.CertificationId })
            .Distinct()
            .Take(5)
            .Select(t => new SearchResultDto("Topic", t.Topic, "Practice this topic", Guid.Empty, t.CertificationId))
            .ToListAsync(ct);
        results.AddRange(topicMatches);

        var questionMatches = await _db.Questions
            .Where(q => q.Prompt.Contains(normalized))
            .Take(5)
            .Select(q => new SearchResultDto("Question", q.Prompt, q.Topic, q.Id, q.CertificationId))
            .ToListAsync(ct);
        results.AddRange(questionMatches);

        return results;
    }
}
