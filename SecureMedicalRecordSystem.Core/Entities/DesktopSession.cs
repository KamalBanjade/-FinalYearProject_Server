using System;
using System.Collections.Generic;

namespace SecureMedicalRecordSystem.Core.Entities;

public class DesktopSession : BaseEntity
{
    public string SessionId { get; set; } = string.Empty; // "desktop-session-xyz789"
    public Guid DoctorId { get; set; }
    public string? ComputerName { get; set; }
    public string? IpAddress { get; set; }
    public string? WebSocketConnectionId { get; set; } // SignalR connection ID
    public DateTime ExpiresAt { get; set; } // 8 hours (work day)
    public DateTime LastActivityAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual Doctor Doctor { get; set; } = null!;
    public virtual ICollection<MobileScannerPairing> MobileScannerPairings { get; set; } = new List<MobileScannerPairing>();
    public virtual ICollection<ScanHistory> ScanHistories { get; set; } = new List<ScanHistory>();
}
