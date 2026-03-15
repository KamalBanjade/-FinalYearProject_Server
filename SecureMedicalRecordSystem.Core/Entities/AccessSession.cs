using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.Entities;

/// <summary>
/// Represents a temporary access session created via a QR token scan.
/// </summary>
public class AccessSession : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string SessionToken { get; set; } = string.Empty;

    [Required]
    public Guid QRTokenId { get; set; }
    public virtual QRToken QRToken { get; set; } = null!;

    [Required]
    public Guid PatientId { get; set; }
    public virtual Patient Patient { get; set; } = null!;

    [Required]
    public DateTime ExpiresAt { get; set; }

    [MaxLength(50)]
    public string IPAddress { get; set; } = string.Empty;

    [MaxLength(500)]
    public string UserAgent { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
