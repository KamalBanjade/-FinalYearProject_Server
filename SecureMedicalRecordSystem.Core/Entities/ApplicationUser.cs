using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = string.Empty;   // "Admin", "Doctor", "Patient"

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool RequiresPasswordChange { get; set; }
    public string? Address { get; set; }
    public string? TOTPSecret { get; set; }
    public bool TOTPSetupCompleted { get; set; } = false;
    public DateTime? TOTPSetupCompletedAt { get; set; }
    public DateTime? MedicalQRGeneratedAt { get; set; }
    public bool TOTPReminderSent { get; set; } = false;

    // OAuth & Profile Extensions
    public string? ProfilePictureUrl { get; set; }
    public string? GoogleId { get; set; }
    public string? Provider { get; set; }

    // Navigation Properties
    // These link to role-specific profiles. They will be null for Admins or non-applicable roles.
    public Patient? PatientProfile { get; set; }
    public Doctor? DoctorProfile { get; set; }
    
    // We can still keep AuditLogs here
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<TrustedDevice> TrustedDevices { get; set; } = new List<TrustedDevice>();
}
