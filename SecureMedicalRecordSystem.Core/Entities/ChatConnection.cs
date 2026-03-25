using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.Entities;

/// <summary>
/// Tracks active SignalR connections per user for presence/online-status awareness.
/// A user may have multiple active connections (phone + desktop).
/// </summary>
public class ChatConnection : BaseEntity
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string ConnectionId { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DisconnectedAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ApplicationUser User { get; set; } = null!;
}
