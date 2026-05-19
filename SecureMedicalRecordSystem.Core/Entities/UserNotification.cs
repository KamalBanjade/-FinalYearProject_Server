namespace SecureMedicalRecordSystem.Core.Entities;

/// <summary>
/// Persisted in-app notification for any user role (Patient, Doctor, Admin).
/// Populated when appointments are created/confirmed/cancelled/rescheduled,
/// or when any system event needs to survive a page refresh.
/// </summary>
public class UserNotification : BaseEntity
{
    public Guid UserId { get; set; }          // The recipient's ApplicationUser.Id
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;  // e.g. AppointmentCreated
    public string? ReferenceId { get; set; }           // e.g. AppointmentId
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
}
