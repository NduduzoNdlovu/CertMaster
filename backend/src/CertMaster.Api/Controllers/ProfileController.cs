using CertMaster.Application.Features.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CertMaster.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly ProfileService _profileService;

    public ProfileController(ProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpPut]
    public async Task<ActionResult> UpdateProfile(UpdateProfileRequest request, CancellationToken ct)
    {
        await _profileService.UpdateProfileAsync(request, ct);
        return Ok(new { message = "Profile updated." });
    }

    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await _profileService.ChangePasswordAsync(request, ct);
        return Ok(new { message = "Password updated." });
    }

    [HttpGet("notification-preferences")]
    public async Task<ActionResult<NotificationPreferencesDto>> GetNotificationPreferences(CancellationToken ct)
    {
        return Ok(await _profileService.GetNotificationPreferencesAsync(ct));
    }

    [HttpPut("notification-preferences")]
    public async Task<ActionResult> UpdateNotificationPreferences(NotificationPreferencesDto request, CancellationToken ct)
    {
        await _profileService.UpdateNotificationPreferencesAsync(request, ct);
        return Ok(new { message = "Preferences updated." });
    }
}
