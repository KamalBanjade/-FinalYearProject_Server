using SecureMedicalRecordSystem.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    
    // Required fields
    [Required]
    [MaxLength(255)]
    public string Action { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [MaxLength(45)]
    public string IPAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;

    // Optional context fields
    public string? Details { get; set; }
    
    [MaxLength(100)]
    public string? EntityType { get; set; }
    
    [MaxLength(100)]
    public string? EntityId { get; set; }

    // Use AuditSeverity Enum directly or store string as per preference, string mapped for ease if needed. We use enum for safety.
    public AuditSeverity Severity { get; set; } = AuditSeverity.Info;

    // Navigation
    public ApplicationUser? User { get; set; }
}
