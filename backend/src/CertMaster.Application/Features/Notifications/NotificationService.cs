using CertMaster.Application.Common.Interfaces;
using CertMaster.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CertMaster.Application.Features.Notifications;

public record NotificationDto(Guid Id, string Title, string Body, string Type, bool IsRead, DateTime CreatedAtUtc);

public class NotificationService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public NotificationService(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<NotificationDto>> GetMyNotificationsAsync(CancellationToken ct)
    {
        if (_currentUser.UserId is null) throw new UnauthorizedAccessException();

        return await _db.Notifications
            .Where(n => n.UserId == _currentUser.UserId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(50)
            .Select(n => new NotificationDto(n.Id, n.Title, n.Body, n.Type, n.IsRead, n.CreatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken ct)
    {
        if (_currentUser.UserId is null) return 0;
        return await _db.Notifications.CountAsync(n => n.UserId == _currentUser.UserId && !n.IsRead, ct);
    }

    public async Task MarkReadAsync(Guid notificationId, CancellationToken ct)
    {
        var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId, ct);
        if (notification is null || notification.UserId != _currentUser.UserId) return;

        notification.IsRead = true;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkAllReadAsync(CancellationToken ct)
    {
        var unread = await _db.Notifications
            .Where(n => n.UserId == _currentUser.UserId && !n.IsRead)
            .ToListAsync(ct);

        foreach (var n in unread) n.IsRead = true;
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Used by other services (exam completion, maintenance scheduling, etc.) to raise a notification.</summary>
    public static Notification Build(Guid userId, string title, string body, string type = "General") => new()
    {
        UserId = userId,
        Title = title,
        Body = body,
        Type = type,
    };
}
