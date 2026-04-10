using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureMedicalRecordSystem.Core.DTOs.Chat;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Infrastructure.Data;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class ChatService : IChatService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ChatService> _logger;

    public ChatService(ApplicationDbContext context, ILogger<ChatService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────
    // MESSAGING
    // ────────────────────────────────────────────────────────────

    public async Task<MessageDTO> SendMessageAsync(Guid senderId, Guid receiverId, string senderRole, string messageText)
    {
        var resolvedReceiverId = await ResolveUserIdAsync(receiverId) ?? receiverId;

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = senderId,
            ReceiverId = resolvedReceiverId,
            SenderRole = senderRole,
            MessageText = messageText.Trim(),
            SentAt = DateTime.UtcNow,
            IsRead = false,
            CreatedBy = senderId.ToString()
        };

        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();

        var sender = await _context.Users.FindAsync(senderId);

        return new MessageDTO
        {
            Id = message.Id,
            SenderId = message.SenderId,
            ReceiverId = message.ReceiverId,
            SenderName = sender != null ? $"{sender.FirstName} {sender.LastName}" : "Unknown",
            SenderRole = message.SenderRole,
            SenderProfilePictureUrl = sender?.ProfilePictureUrl,
            MessageText = message.MessageText,
            SentAt = message.SentAt,
            IsRead = message.IsRead
        };
    }

    public async Task<List<MessageDTO>> GetConversationAsync(Guid userId, Guid otherUserId, int page = 1, int pageSize = 50)
    {
        var resolvedOtherUserId = await ResolveUserIdAsync(otherUserId) ?? otherUserId;

        var messages = await _context.ChatMessages
            .IgnoreQueryFilters() // we apply our own IsDeleted check below
            .Where(m => !m.IsDeleted &&
                        ((m.SenderId == userId && m.ReceiverId == resolvedOtherUserId) ||
                         (m.SenderId == resolvedOtherUserId && m.ReceiverId == userId)))
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MessageDTO
            {
                Id = m.Id,
                SenderId = m.SenderId,
                ReceiverId = m.ReceiverId,
                SenderName = m.Sender.FirstName + " " + m.Sender.LastName,
                SenderRole = m.SenderRole,
                SenderProfilePictureUrl = m.Sender.ProfilePictureUrl,
                MessageText = m.MessageText,
                SentAt = m.SentAt,
                IsRead = m.IsRead,
                ReadAt = m.ReadAt,
                IsEdited = m.IsEdited
            })
            .ToListAsync();

        // Return in chronological order (newest at bottom)
        return messages.OrderBy(m => m.SentAt).ToList();
    }

    public async Task<List<ConversationDTO>> GetUserConversationsAsync(Guid userId)
    {
        var raw = await _context.ChatMessages
            .IgnoreQueryFilters()
            .Where(m => !m.IsDeleted && (m.SenderId == userId || m.ReceiverId == userId))
            .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
            .Select(g => new
            {
                OtherUserId = g.Key,
                LastMessageText = g.OrderByDescending(m => m.SentAt).Select(m => m.MessageText).FirstOrDefault(),
                LastMessageAt = (DateTime?)g.Max(m => m.SentAt),
                UnreadCount = g.Count(m => m.ReceiverId == userId && !m.IsRead)
            })
            .ToListAsync();

        var dtos = new List<ConversationDTO>();
        foreach (var item in raw)
        {
            var otherUser = await _context.Users.FindAsync(item.OtherUserId);
            if (otherUser == null) continue;

            dtos.Add(new ConversationDTO
            {
                OtherUserId = item.OtherUserId,
                OtherUserName = $"{otherUser.FirstName} {otherUser.LastName}",
                OtherUserRole = otherUser.Role,
                OtherUserProfilePictureUrl = otherUser.ProfilePictureUrl,
                LastMessageText = item.LastMessageText,
                LastMessageAt = item.LastMessageAt,
                UnreadCount = item.UnreadCount,
                IsOnline = await IsUserOnlineAsync(item.OtherUserId.ToString()),
                LastSeenAt = await GetLastSeenAsync(item.OtherUserId.ToString())
            });
        }

        return dtos.OrderByDescending(c => c.LastMessageAt).ToList();
    }

    public async Task<MessageDTO> GetMessageAsync(Guid messageId)
    {
        var message = await _context.ChatMessages
            .IgnoreQueryFilters()
            .Include(m => m.Sender)
            .FirstOrDefaultAsync(m => m.Id == messageId)
            ?? throw new KeyNotFoundException($"Message {messageId} not found.");

        return new MessageDTO
        {
            Id = message.Id,
            SenderId = message.SenderId,
            ReceiverId = message.ReceiverId,
            SenderName = $"{message.Sender.FirstName} {message.Sender.LastName}",
            SenderRole = message.SenderRole,
            SenderProfilePictureUrl = message.Sender.ProfilePictureUrl,
            MessageText = message.MessageText,
            SentAt = message.SentAt,
            IsRead = message.IsRead,
            ReadAt = message.ReadAt,
            IsEdited = message.IsEdited
        };
    }

    public async Task MarkAsReadAsync(Guid messageId, Guid readerId)
    {
        var message = await _context.ChatMessages
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == messageId);

        if (message != null && message.ReceiverId == readerId && !message.IsRead)
        {
            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
            message.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _context.ChatMessages
            .IgnoreQueryFilters()
            .CountAsync(m => m.ReceiverId == userId && !m.IsRead && !m.IsDeleted);
    }

    public async Task<bool> DeleteConversationAsync(Guid currentUserId, Guid otherUserId)
    {
        var resolvedOtherUserId = await ResolveUserIdAsync(otherUserId) ?? otherUserId;

        var messages = await _context.ChatMessages
            .Where(m => (m.SenderId == currentUserId && m.ReceiverId == resolvedOtherUserId) ||
                        (m.SenderId == resolvedOtherUserId && m.ReceiverId == currentUserId))
            .ToListAsync();

        if (messages.Any())
        {
            _context.ChatMessages.RemoveRange(messages);
            await _context.SaveChangesAsync();
        }

        return true;
    }

    // ────────────────────────────────────────────────────────────
    // AUTHORIZATION
    // ────────────────────────────────────────────────────────────

    public async Task<bool> CanUsersMessageAsync(string senderId, string receiverId)
    {
        if (!Guid.TryParse(senderId, out var senderGuid) ||
            !Guid.TryParse(receiverId, out var receiverGuid))
            return false;

        var resolvedSenderId = await ResolveUserIdAsync(senderGuid);
        var resolvedReceiverId = await ResolveUserIdAsync(receiverGuid);

        if (resolvedSenderId == null || resolvedReceiverId == null) return false;

        var sender = await _context.Users.FindAsync(resolvedSenderId);
        var receiver = await _context.Users.FindAsync(resolvedReceiverId);

        if (sender == null || receiver == null) return false;

        bool isValidPair = (sender.Role == "Doctor" && receiver.Role == "Patient") ||
                           (sender.Role == "Patient" && receiver.Role == "Doctor");
                           
        return isValidPair;
    }

    private async Task<Guid?> ResolveUserIdAsync(Guid id)
    {
        // 1. Check if it's already a UserId
        if (await _context.Users.AnyAsync(u => u.Id == id))
            return id;

        // 2. Check if it's a DoctorId
        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == id);
        if (doctor != null) return doctor.UserId;

        // 3. Check if it's a PatientId
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == id);
        if (patient != null) return patient.UserId;

        return null;
    }


    // ────────────────────────────────────────────────────────────
    // CONNECTION TRACKING
    // ────────────────────────────────────────────────────────────

    public async Task TrackConnectionAsync(string userId, string connectionId, string? userAgent)
    {
        if (!Guid.TryParse(userId, out var userGuid)) return;

        var connection = new ChatConnection
        {
            Id = Guid.NewGuid(),
            UserId = userGuid,
            ConnectionId = connectionId,
            UserAgent = userAgent,
            ConnectedAt = DateTime.UtcNow,
            IsActive = true,
            CreatedBy = userId
        };

        _context.ChatConnections.Add(connection);
        await _context.SaveChangesAsync();
        _logger.LogDebug("Tracked connection {ConnectionId} for user {UserId}", connectionId, userId);
    }

    public async Task RemoveConnectionAsync(string connectionId)
    {
        var connection = await _context.ChatConnections
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.ConnectionId == connectionId);

        if (connection != null)
        {
            connection.IsActive = false;
            connection.DisconnectedAt = DateTime.UtcNow;
            connection.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogDebug("Removed connection {ConnectionId}", connectionId);
        }
    }

    public async Task<bool> IsUserOnlineAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid)) return false;

        return await _context.ChatConnections
            .IgnoreQueryFilters()
            .AnyAsync(c => c.UserId == userGuid && c.IsActive && !c.IsDeleted);
    }

    public async Task<DateTime?> GetLastSeenAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid)) return null;

        var lastConn = await _context.ChatConnections
            .IgnoreQueryFilters()
            .Where(c => c.UserId == userGuid && !c.IsDeleted)
            .OrderByDescending(c => c.ConnectedAt)
            .FirstOrDefaultAsync();

        if (lastConn != null && lastConn.IsActive)
            return DateTime.UtcNow;

        return lastConn?.DisconnectedAt;
    }
}