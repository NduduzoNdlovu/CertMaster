using CertMaster.Application.Features.Bookmarks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CertMaster.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/bookmarks")]
public class BookmarksController : ControllerBase
{
    private readonly BookmarkService _bookmarkService;

    public BookmarksController(BookmarkService bookmarkService)
    {
        _bookmarkService = bookmarkService;
    }

    [HttpGet]
    public async Task<ActionResult<List<BookmarkedQuestionDto>>> GetMyBookmarks(CancellationToken ct)
    {
        return Ok(await _bookmarkService.GetMyBookmarksAsync(ct));
    }

    [HttpGet("ids")]
    public async Task<ActionResult<HashSet<Guid>>> GetMyBookmarkedIds(CancellationToken ct)
    {
        return Ok(await _bookmarkService.GetMyBookmarkedQuestionIdsAsync(ct));
    }

    [HttpPost("{questionId:guid}/toggle")]
    public async Task<ActionResult> Toggle(Guid questionId, CancellationToken ct)
    {
        var isBookmarked = await _bookmarkService.ToggleAsync(questionId, ct);
        return Ok(new { isBookmarked });
    }
}
