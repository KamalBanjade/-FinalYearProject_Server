using SecureMedicalRecordSystem.Core.Enums;

namespace SecureMedicalRecordSystem.Core.Entities;

public class Appointment : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }

    // Timestamps
    public DateTime ScheduledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    // State
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Requested;

    // Details
    public string? ReasonForVisit { get; set; }
    public string? ConsultationNotes { get; set; }
    public Guid? LinkedRecordId { get; set; }

    // Navigation
    public Patient Patient { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
}
