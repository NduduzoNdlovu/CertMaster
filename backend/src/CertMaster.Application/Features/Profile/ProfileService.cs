using CertMaster.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CertMaster.Application.Features.Profile;

public record UpdateProfileRequest(string FullName);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record NotificationPreferencesDto(bool DailyReminders, bool WeeklySummary, bool ProductUpdates);

public class ProfileService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _clock;

    public ProfileService(
        IApplicationDbContext db, ICurrentUserService currentUser, IPasswordHasher passwordHasher, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
        _clock = clock;
    }

    public async Task UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct)
    {
        if (_currentUser.UserId is null) throw new UnauthorizedAccessException();
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new InvalidOperationException("Name cannot be empty.");

        var user = await _db.Users.FirstAsync(u => u.Id == _currentUser.UserId, ct);
        user.FullName = request.FullName.Trim();
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Changes the password and revokes every existing refresh token for this user, so any
    /// other logged-in session (another browser/device) is forced to log in again with the
    /// new password rather than silently continuing on the old one.
    /// </summary>
    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct)
    {
        if (_currentUser.UserId is null) throw new UnauthorizedAccessException();

        var user = await _db.Users.FirstAsync(u => u.Id == _currentUser.UserId, ct);

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new InvalidOperationException("Current password is incorrect.");

        if (request.NewPassword.Length < 8)
            throw new InvalidOperationException("New password must be at least 8 characters.");

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);

        var activeTokens = await _db.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAtUtc == null)
            .ToListAsync(ct);
        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = _clock.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<NotificationPreferencesDto> GetNotificationPreferencesAsync(CancellationToken ct)
    {
        if (_currentUser.UserId is null) throw new UnauthorizedAccessException();

        var user = await _db.Users.FirstAsync(u => u.Id == _currentUser.UserId, ct);
        return new NotificationPreferencesDto(user.DailyRemindersEnabled, user.WeeklySummaryEnabled, user.ProductUpdatesEnabled);
    }

    public async Task UpdateNotificationPreferencesAsync(NotificationPreferencesDto request, CancellationToken ct)
    {
        if (_currentUser.UserId is null) throw new UnauthorizedAccessException();

        var user = await _db.Users.FirstAsync(u => u.Id == _currentUser.UserId, ct);
        user.DailyRemindersEnabled = request.DailyReminders;
        user.WeeklySummaryEnabled = request.WeeklySummary;
        user.ProductUpdatesEnabled = request.ProductUpdates;

        await _db.SaveChangesAsync(ct);
    }
}
