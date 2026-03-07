using System.ComponentModel.DataAnnotations;
using SecureMedicalRecordSystem.Core.Enums;

namespace SecureMedicalRecordSystem.Core.Entities;

public class DoctorAvailability : BaseEntity
{
    public Guid DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;

    public int? DayOfWeek { get; set; } // 0-6 (Sunday-Saturday)
    public DateTime? SpecificDate { get; set; }

    [Required]
    public TimeSpan StartTime { get; set; }

    [Required]
    public TimeSpan EndTime { get; set; }

    public bool IsAvailable { get; set; } = true;

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public RecurrenceType RecurrenceType { get; set; }

    [MaxLength(100)]
    public string? Reason { get; set; } // "Lunch Break", "Vacation", etc.
}
