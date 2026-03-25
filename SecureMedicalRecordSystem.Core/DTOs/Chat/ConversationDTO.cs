namespace SecureMedicalRecordSystem.Core.DTOs.Chat;

public class ConversationDTO
{
    public Guid OtherUserId { get; set; }
    public string OtherUserName { get; set; } = string.Empty;
    public string OtherUserRole { get; set; } = string.Empty;
    public string? OtherUserProfilePictureUrl { get; set; }
    public string? LastMessageText { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSeenAt { get; set; }
}
