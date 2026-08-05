using CertMaster.Application.Common.Interfaces;
using CertMaster.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CertMaster.Application.Features.Bookmarks;

public record BookmarkedQuestionDto(
    Guid QuestionId,
    Guid CertificationId,
    string Topic,
    string Difficulty,
    string Prompt,
    DateTime BookmarkedAtUtc);

public class BookmarkService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public BookmarkService(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<BookmarkedQuestionDto>> GetMyBookmarksAsync(CancellationToken ct)
    {
        if (_currentUser.UserId is null) throw new UnauthorizedAccessException();

        return await _db.Bookmarks
            .Where(b => b.UserId == _currentUser.UserId)
            .OrderByDescending(b => b.CreatedAtUtc)
            .Select(b => new BookmarkedQuestionDto(
                b.QuestionId,
                b.Question!.CertificationId,
                b.Question.Topic,
                b.Question.Difficulty.ToString(),
                b.Question.Prompt,
                b.CreatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<HashSet<Guid>> GetMyBookmarkedQuestionIdsAsync(CancellationToken ct)
    {
        if (_currentUser.UserId is null) return new HashSet<Guid>();

        var ids = await _db.Bookmarks
            .Where(b => b.UserId == _currentUser.UserId)
            .Select(b => b.QuestionId)
            .ToListAsync(ct);

        return ids.ToHashSet();
    }

    /// <summary>Adds the bookmark if it doesn't exist, removes it if it does. Returns the new state.</summary>
    public async Task<bool> ToggleAsync(Guid questionId, CancellationToken ct)
    {
        if (_currentUser.UserId is null) throw new UnauthorizedAccessException();

        var existing = await _db.Bookmarks
            .FirstOrDefaultAsync(b => b.UserId == _currentUser.UserId && b.QuestionId == questionId, ct);

        if (existing is not null)
        {
            _db.Bookmarks.Remove(existing);
            await _db.SaveChangesAsync(ct);
            return false;
        }

        var questionExists = await _db.Questions.AnyAsync(q => q.Id == questionId, ct);
        if (!questionExists) throw new InvalidOperationException("Question not found.");

        _db.Bookmarks.Add(new Bookmark { UserId = _currentUser.UserId.Value, QuestionId = questionId });
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
