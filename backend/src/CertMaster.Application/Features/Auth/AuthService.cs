using System.Security.Cryptography;
using CertMaster.Application.Common.Interfaces;
using CertMaster.Application.Common.Models;
using CertMaster.Application.Features.Notifications;
using CertMaster.Domain.Entities;
using CertMaster.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CertMaster.Application.Features.Auth;

public class AuthService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _clock;
    private readonly IEmailSender _emailSender;

    public AuthService(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IDateTimeProvider clock,
        IEmailSender emailSender)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _clock = clock;
        _emailSender = emailSender;
    }

    public async Task<AuthResultDto> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var exists = await _db.Users.AnyAsync(u => u.Email == normalizedEmail, ct);
        if (exists)
        {
            throw new InvalidOperationException("An account with this email already exists.");
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.Learner,
            EmailConfirmationToken = GenerateToken(),
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        _db.Notifications.Add(NotificationService.Build(
            user.Id,
            "Welcome to CertMaster!",
            "Start with a practice session on CompTIA A+ Core 1, Core 2, or Network+ to see where you stand.",
            "General"));
        await _db.SaveChangesAsync(ct);

        await _emailSender.SendAsync(
            user.Email,
            "Confirm your CertMaster email address",
            $"<p>Welcome to CertMaster! Confirm your email using this verification code:</p><p><strong>{user.EmailConfirmationToken}</strong></p>",
            ct);

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResultDto> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Incorrect email or password.");
        }

        if (user.IsSuspended)
        {
            throw new UnauthorizedAccessException("This account has been suspended.");
        }

        user.LastActivityAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResultDto> RefreshAsync(RefreshRequest request, CancellationToken ct)
    {
        var token = await _db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, ct);

        if (token is null || !token.IsActive || token.User is null)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        token.RevokedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);

        return await IssueTokensAsync(token.User, ct);
    }

    public async Task ConfirmEmailAsync(string token, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.EmailConfirmationToken == token, ct);
        if (user is null)
            throw new InvalidOperationException("Invalid or expired confirmation code.");

        user.EmailConfirmed = true;
        user.EmailConfirmationToken = null;
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Always completes without revealing whether the email exists, to avoid
    /// account enumeration. Only sends an email if a matching account is found.
    /// </summary>
    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);
        if (user is null) return;

        user.PasswordResetToken = GenerateToken();
        user.PasswordResetTokenExpiresAtUtc = _clock.UtcNow.AddMinutes(30);
        await _db.SaveChangesAsync(ct);

        await _emailSender.SendAsync(
            user.Email,
            "Reset your CertMaster password",
            $"<p>Use this code to reset your password (expires in 30 minutes):</p><p><strong>{user.PasswordResetToken}</strong></p>",
            ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token, ct);

        if (user is null || user.PasswordResetTokenExpiresAtUtc is null || user.PasswordResetTokenExpiresAtUtc < _clock.UtcNow)
        {
            throw new InvalidOperationException("Invalid or expired reset code.");
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiresAtUtc = null;
        await _db.SaveChangesAsync(ct);
    }

    private async Task<AuthResultDto> IssueTokensAsync(User user, CancellationToken ct)
    {
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshTokenValue = _jwtTokenService.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAtUtc = _clock.UtcNow.Add(_jwtTokenService.RefreshTokenLifetime),
        });

        await _db.SaveChangesAsync(ct);

        var isPremium = user.Plan != SubscriptionPlan.Free &&
                         (user.PremiumExpiresAtUtc is null || user.PremiumExpiresAtUtc > _clock.UtcNow);

        var userDto = new UserDto(user.Id, user.FullName, user.Email, user.Role.ToString(), isPremium);
        return new AuthResultDto(accessToken, refreshTokenValue, userDto);
    }

    private static string GenerateToken()
    {
        // 6-digit numeric code — easy to type/paste for email confirmation and password reset.
        var bytes = RandomNumberGenerator.GetBytes(4);
        var value = BitConverter.ToUInt32(bytes, 0) % 1_000_000;
        return value.ToString("D6");
    }
}
