using SecureMedicalRecordSystem.Core.DTOs.Chat;

namespace SecureMedicalRecordSystem.Core.Interfaces;

public interface IChatService
{
    // ── Messaging ──────────────────────────────────────────────────
    Task<MessageDTO> SendMessageAsync(Guid senderId, Guid receiverId, string senderRole, string messageText);
    Task<List<MessageDTO>> GetConversationAsync(Guid userId, Guid otherUserId, int page = 1, int pageSize = 50);
    Task<List<ConversationDTO>> GetUserConversationsAsync(Guid userId);
    Task<MessageDTO> GetMessageAsync(Guid messageId);
    Task MarkAsReadAsync(Guid messageId, Guid readerId);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task<bool> DeleteConversationAsync(Guid currentUserId, Guid otherUserId);

    // ── Authorization ──────────────────────────────────────────────
    /// <summary>Returns true when the two users share a doctor-patient medical relationship.</summary>
    Task<bool> CanUsersMessageAsync(string senderId, string receiverId);

    // ── Connection Tracking / Presence ─────────────────────────────
    Task TrackConnectionAsync(string userId, string connectionId, string? userAgent);
    Task RemoveConnectionAsync(string connectionId);
    Task<bool> IsUserOnlineAsync(string userId);
    Task<DateTime?> GetLastSeenAsync(string userId);
}
