namespace SecureMedicalRecordSystem.Core.Entities;

/// <summary>
/// Immutable audit trail for HIPAA compliance.
/// Every create/read/update/delete action is logged here.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;       // e.g., "READ_RECORD", "UPDATE_PATIENT"
    public string EntityType { get; set; } = string.Empty;   // e.g., "MedicalRecord"
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }  // JSON snapshot
    public string? NewValues { get; set; }  // JSON snapshot
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsSuccess { get; set; } = true;
    public string? FailureReason { get; set; }

    // Navigation
    public User? User { get; set; }
}
