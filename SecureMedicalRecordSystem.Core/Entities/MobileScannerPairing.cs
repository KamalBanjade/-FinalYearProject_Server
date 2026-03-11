using System;

namespace SecureMedicalRecordSystem.Core.Entities;

public class MobileScannerPairing : BaseEntity
{
    public string MobileDeviceId { get; set; } = string.Empty; // Device fingerprint
    public Guid DesktopSessionId { get; set; }
    public Guid DoctorId { get; set; }
    public string? DeviceName { get; set; } // "iPhone 15 Pro"
    public DateTime PairedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } // 30 days
    public DateTime? LastUsedAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual DesktopSession DesktopSession { get; set; } = null!;
    public virtual Doctor Doctor { get; set; } = null!;
}
