using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SecureMedicalRecordSystem.Core.Enums;

namespace SecureMedicalRecordSystem.Core.Entities;

public class Appointment : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    
    [MaxLength(500)]
    public string? ReasonForVisit { get; set; }
    public string? ConsultationNotes { get; set; }
    public int Duration { get; set; } = 30;

    // Status Tracking
    public new DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ScheduledAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    
    [MaxLength(200)]
    public string? CancellationReason { get; set; }

    // Metadata
    public bool IsActive { get; set; } = true;
    public bool IsCancelled { get; set; } = false;
    public bool IsCompleted { get; set; } = false;
    public bool ReminderSent { get; set; } = false;
    public new Guid CreatedBy { get; set; }

    // Follow-Up Tracking
    public Guid? ParentAppointmentId { get; set; }

    // Navigation
    public Patient Patient { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
    public ICollection<AppointmentRecord> LinkedRecords { get; set; } = new List<AppointmentRecord>();

    [ForeignKey("ParentAppointmentId")]
    public virtual Appointment? ParentAppointment { get; set; }
}
