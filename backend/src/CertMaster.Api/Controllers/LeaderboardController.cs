using CertMaster.Application.Features.Leaderboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CertMaster.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/leaderboard")]
public class LeaderboardController : ControllerBase
{
    private readonly LeaderboardService _leaderboardService;

    public LeaderboardController(LeaderboardService leaderboardService)
    {
        _leaderboardService = leaderboardService;
    }

    [HttpGet]
    public async Task<ActionResult<List<LeaderboardEntryDto>>> GetTop(
        [FromQuery] int take = 10, [FromQuery] LeaderboardPeriod period = LeaderboardPeriod.AllTime, CancellationToken ct = default)
    {
        return Ok(await _leaderboardService.GetTopLearnersAsync(take, period, ct));
    }

    [HttpGet("me")]
    public async Task<ActionResult<LeaderboardEntryDto?>> GetMyRank(
        [FromQuery] LeaderboardPeriod period = LeaderboardPeriod.AllTime, CancellationToken ct = default)
    {
        return Ok(await _leaderboardService.GetMyRankAsync(period, ct));
    }
}
