using System;

namespace SecureMedicalRecordSystem.Core.Entities;

public class ScanHistory : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid? DesktopSessionId { get; set; }
    public string? MobileDeviceId { get; set; }
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
    public bool TOTPVerified { get; set; }
    public DateTime? TOTPVerifiedAt { get; set; }
    public bool AccessGranted { get; set; }

    // Navigation properties
    public virtual Patient Patient { get; set; } = null!;
    public virtual Doctor Doctor { get; set; } = null!;
    public virtual DesktopSession? DesktopSession { get; set; }
}
