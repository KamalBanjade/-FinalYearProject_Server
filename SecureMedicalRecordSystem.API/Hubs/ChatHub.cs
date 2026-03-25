using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SecureMedicalRecordSystem.Core.Interfaces;
using System.Security.Claims;

namespace SecureMedicalRecordSystem.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────
    // CONNECTION LIFECYCLE
    // ────────────────────────────────────────────────────────────

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        _logger.LogInformation("User {UserId} connected via ChatHub ({ConnectionId})", userId, Context.ConnectionId);

        await _chatService.TrackConnectionAsync(
            userId,
            Context.ConnectionId,
            Context.GetHttpContext()?.Request.Headers["User-Agent"].ToString());

        // Notify all other connected clients that this user is now online
        await Clients.Others.SendAsync("UserOnline", userId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        _logger.LogInformation("User {UserId} disconnected from ChatHub ({ConnectionId})", userId, Context.ConnectionId);

        await _chatService.RemoveConnectionAsync(Context.ConnectionId);

        // If no other active connections remain, broadcast offline status
        var isStillOnline = await _chatService.IsUserOnlineAsync(userId);
        if (!isStillOnline)
        {
            await Clients.Others.SendAsync("UserOffline", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    // ────────────────────────────────────────────────────────────
    // MESSAGING
    // ────────────────────────────────────────────────────────────

    /// <summary>Send a private message to another user (real-time + persisted).</summary>
    public async Task SendMessage(string receiverId, string messageText)
    {
        var senderId = GetUserId();
        var senderRole = GetUserRole();

        if (string.IsNullOrWhiteSpace(messageText))
            throw new HubException("Message text cannot be empty.");

        var canMessage = await _chatService.CanUsersMessageAsync(senderId, receiverId);
        if (!canMessage)
        {
            _logger.LogWarning("Unauthorized messaging attempt: {SenderId} → {ReceiverId}", senderId, receiverId);
            throw new HubException("You are not authorized to message this user.");
        }

        var message = await _chatService.SendMessageAsync(
            senderId: Guid.Parse(senderId),
            receiverId: Guid.Parse(receiverId),
            senderRole: senderRole,
            messageText: messageText);

        // Push to recipient (real-time)
        await Clients.User(receiverId).SendAsync("ReceiveMessage", new
        {
            id = message.Id,
            senderId = message.SenderId,
            senderName = message.SenderName,
            senderRole = message.SenderRole,
            messageText = message.MessageText,
            sentAt = message.SentAt,
            isRead = false
        });

        // Confirm back to all sender's devices
        await Clients.Caller.SendAsync("MessageSent", new
        {
            id = message.Id,
            receiverId = message.ReceiverId,
            messageText = message.MessageText,
            sentAt = message.SentAt
        });

        _logger.LogInformation("Message {MessageId}: {SenderId} → {ReceiverId}", message.Id, senderId, receiverId);
    }

    /// <summary>Mark a received message as read and notify the original sender.</summary>
    public async Task MarkMessageAsRead(string messageId)
    {
        var userId = GetUserId();
        if (!Guid.TryParse(messageId, out var msgGuid)) return;

        await _chatService.MarkAsReadAsync(msgGuid, Guid.Parse(userId));

        var message = await _chatService.GetMessageAsync(msgGuid);
        await Clients.User(message.SenderId.ToString()).SendAsync("MessageRead", new
        {
            messageId,
            readAt = DateTime.UtcNow,
            readBy = userId
        });
    }

    // ────────────────────────────────────────────────────────────
    // TYPING INDICATORS
    // ────────────────────────────────────────────────────────────

    public async Task SendTypingIndicator(string receiverId)
    {
        var senderId = GetUserId();
        await Clients.User(receiverId).SendAsync("UserTyping", senderId);
    }

    public async Task StopTypingIndicator(string receiverId)
    {
        var senderId = GetUserId();
        await Clients.User(receiverId).SendAsync("UserStoppedTyping", senderId);
    }

    // ────────────────────────────────────────────────────────────
    // PRESENCE
    // ────────────────────────────────────────────────────────────

    public async Task<bool> CheckUserOnlineStatus(string userId) =>
        await _chatService.IsUserOnlineAsync(userId);

    // ────────────────────────────────────────────────────────────
    // HELPERS
    // ────────────────────────────────────────────────────────────

    private string GetUserId() =>
        Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new HubException("User ID not found in token.");

    private string GetUserRole() =>
        Context.User?.FindFirst(ClaimTypes.Role)?.Value
        ?? throw new HubException("User role not found in token.");
}
