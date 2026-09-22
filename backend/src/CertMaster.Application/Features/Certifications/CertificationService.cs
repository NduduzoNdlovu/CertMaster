using System.Text.Json;
using CertMaster.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace CertMaster.Application.Features.Certifications;

public record CertificationDto(
    Guid Id,
    string Code,
    string Name,
    string Version,
    string Description,
    int TopicCount,
    int QuestionCount,
    int ExamDurationMinutes,
    int PassingScorePercent,
    int MockExamQuestionCount);

public class CertificationService
{
    private const string CacheKey = "certifications:active";
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
    };

    private readonly IApplicationDbContext _db;
    private readonly IDistributedCache _cache;

    public CertificationService(IApplicationDbContext db, IDistributedCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<List<CertificationDto>> GetActiveCertificationsAsync(CancellationToken ct)
    {
        // Certifications change rarely (only when an admin adds/edits one or uploads a
        // new question bank), so this listing is a good candidate for short-lived caching
        // to keep the most-hit endpoint fast under load. Backed by Redis in production;
        // falls back to an in-process cache automatically if Redis isn't configured.
        var cached = await _cache.GetStringAsync(CacheKey, ct);
        if (cached is not null)
        {
            return JsonSerializer.Deserialize<List<CertificationDto>>(cached) ?? new List<CertificationDto>();
        }

        var certifications = await _db.Certifications
            .Where(c => c.IsActive)
            .Select(c => new CertificationDto(
                c.Id,
                c.Code,
                c.Name,
                c.CurrentVersion,
                c.Description,
                c.Topics.Count,
                c.Questions.Count(q => q.Status == Domain.Enums.QuestionStatus.Published),
                c.ExamDurationMinutes,
                c.PassingScorePercent,
                c.MockExamQuestionCount))
            .ToListAsync(ct);

        await _cache.SetStringAsync(CacheKey, JsonSerializer.Serialize(certifications), CacheOptions, ct);

        return certifications;
    }

    /// <summary>Called after a dump upload or admin edit so the next request reflects new data immediately.</summary>
    public Task InvalidateCacheAsync(CancellationToken ct) => _cache.RemoveAsync(CacheKey, ct);
}
