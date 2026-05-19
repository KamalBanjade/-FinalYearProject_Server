using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SecureMedicalRecordSystem.API.Hubs;
using SecureMedicalRecordSystem.Core.DTOs.Analysis;
using SecureMedicalRecordSystem.Core.DTOs.Notifications;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Infrastructure.Data;

namespace SecureMedicalRecordSystem.API.Services;

public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ApplicationDbContext _context;

    public SignalRNotificationService(
        IHubContext<NotificationHub> hubContext,
        ApplicationDbContext context)
    {
        _hubContext = hubContext;
        _context = context;
    }

    // --- Stability Alerts (always persisted separately via StabilityAlertService) ---
    public async Task SendStabilityAlertAsync(Guid doctorId, StabilityAlertDto alert)
    {
        await _hubContext.Clients
            .User(doctorId.ToString())
            .SendAsync("ReceiveStabilityAlert", alert);
    }

    // --- Fire-and-forget SignalR only (no DB persistence) ---
    public async Task SendNotificationAsync(Guid userId, SystemNotificationDto notification)
    {
        await _hubContext.Clients
            .User(userId.ToString())
            .SendAsync("ReceiveNotification", notification);
    }

    // --- Persist to DB THEN push live via SignalR ---
    public async Task PersistAndSendNotificationAsync(Guid userId, SystemNotificationDto notification)
    {
        // 1. Save to database
        var entity = new UserNotification
        {
            Id = notification.Id,
            UserId = userId,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            ReferenceId = notification.ReferenceId,
            IsRead = false,
            CreatedAt = notification.CreatedAt,
            CreatedBy = "system"
        };
        _context.UserNotifications.Add(entity);
        await _context.SaveChangesAsync();

        // 2. Push live to connected client
        notification.IsRead = false;
        await _hubContext.Clients
            .User(userId.ToString())
            .SendAsync("ReceiveNotification", notification);
    }

    public async Task<List<SystemNotificationDto>> GetRecentNotificationsAsync(Guid userId, int count = 30)
    {
        return await _context.UserNotifications
            .Where(n => n.UserId == userId && !n.IsDeleted)
            .OrderByDescending(n => n.CreatedAt)
            .Take(count)
            .Select(n => new SystemNotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                ReferenceId = n.ReferenceId,
                CreatedAt = n.CreatedAt,
                IsRead = n.IsRead
            })
            .ToListAsync();
    }

    public async Task MarkNotificationAsReadAsync(Guid notificationId, Guid userId)
    {
        var notification = await _context.UserNotifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId && !n.IsDeleted);

        if (notification == null) return;

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        notification.UpdatedAt = DateTime.UtcNow;
        notification.UpdatedBy = userId.ToString();
        await _context.SaveChangesAsync();
    }

    public async Task MarkAllNotificationsAsReadAsync(Guid userId)
    {
        var unread = await _context.UserNotifications
            .Where(n => n.UserId == userId && !n.IsRead && !n.IsDeleted)
            .ToListAsync();

        if (unread.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = now;
            n.UpdatedAt = now;
            n.UpdatedBy = userId.ToString();
        }
        await _context.SaveChangesAsync();
    }
}
