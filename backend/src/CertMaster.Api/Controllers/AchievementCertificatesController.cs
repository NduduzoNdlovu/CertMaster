using System.Security.Claims;
using CertMaster.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CertMaster.Api.Controllers;

[ApiController]
[Route("api/achievement-certificates")]
public sealed class AchievementCertificatesController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    public AchievementCertificatesController(IApplicationDbContext db) => _db = db;

    [Authorize]
    [HttpGet("mine")]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        var rawId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(rawId, out var userId)) return Unauthorized();
        var certificates = await _db.AchievementCertificates.AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.IssuedAtUtc)
            .Select(c => new { c.Id, c.CertificationId, certificationName = c.Certification!.Name, c.ExamAttemptId, c.ScorePercent, c.Tier, c.VerificationCode, c.IssuedAtUtc })
            .ToListAsync(ct);
        return Ok(certificates);
    }

    [AllowAnonymous]
    [HttpGet("verify/{code}")]
    public async Task<IActionResult> Verify(string code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code)) return BadRequest();
        var certificate = await _db.AchievementCertificates.AsNoTracking()
            .Where(c => c.VerificationCode == code.ToUpperInvariant())
            .Select(c => new { certificateType = "CertMaster Achievement Certificate", c.VerificationCode, learnerName = c.User!.FullName, certificationName = c.Certification!.Name, c.ScorePercent, c.Tier, c.IssuedAtUtc, issuer = "CertMaster" })
            .FirstOrDefaultAsync(ct);
        return certificate is null ? NotFound(new { message = "Certificate not found." }) : Ok(certificate);
    }
}
