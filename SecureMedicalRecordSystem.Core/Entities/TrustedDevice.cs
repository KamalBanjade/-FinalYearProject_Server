using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.Entities;

public class TrustedDevice
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string DeviceToken { get; set; } = string.Empty;

    [MaxLength(100)]
    public string DeviceName { get; set; } = string.Empty;

    public string DeviceFingerprint { get; set; } = string.Empty; // JSON

    [MaxLength(45)]
    public string IPAddress { get; set; } = string.Empty;

    public string UserAgent { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? RevokedAt { get; set; }

    [MaxLength(200)]
    public string? RevokedReason { get; set; }

    // Navigation Properties
    public ApplicationUser User { get; set; } = null!;
}
