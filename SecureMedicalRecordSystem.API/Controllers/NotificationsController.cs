using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureMedicalRecordSystem.Core.Interfaces;
using System.Security.Claims;

namespace SecureMedicalRecordSystem.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>Returns the 30 most recent notifications (read + unread) for the current user.</summary>
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var notifications = await _notificationService.GetRecentNotificationsAsync(userId.Value, 30);
        return Ok(notifications);
    }

    /// <summary>Returns the count of unread notifications for the badge number.</summary>
    [HttpGet("unread/count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var all = await _notificationService.GetRecentNotificationsAsync(userId.Value, 100);
        var count = all.Count(n => !n.IsRead);
        return Ok(new { count });
    }

    /// <summary>Marks a single notification as read.</summary>
    [HttpPatch("{notificationId}/read")]
    public async Task<IActionResult> MarkAsRead(Guid notificationId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        await _notificationService.MarkNotificationAsReadAsync(notificationId, userId.Value);
        return NoContent();
    }

    /// <summary>Marks all notifications as read (called when the bell is opened).</summary>
    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        await _notificationService.MarkAllNotificationsAsReadAsync(userId.Value);
        return NoContent();
    }

    private Guid? GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
