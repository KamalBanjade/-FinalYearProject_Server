using System.ComponentModel.DataAnnotations;
using SecureMedicalRecordSystem.Core.Enums;

namespace SecureMedicalRecordSystem.Core.DTOs.Appointments;

public class DoctorAvailabilityDTO
{
    public Guid Id { get; set; }
    public Guid DoctorId { get; set; }
    public int? DayOfWeek { get; set; }
    public DateTime? SpecificDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsAvailable { get; set; }
    public RecurrenceType RecurrenceType { get; set; }
    public string? Reason { get; set; }
}

public class TimeSlotDTO
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsAvailable { get; set; }
}

public class SetWorkingHoursDTO
{
    [Required]
    public DayOfWeek DayOfWeek { get; set; }
    
    [Required]
    public string StartTime { get; set; } // "09:00"
    
    [Required]
    public string EndTime { get; set; } // "17:00"
}

public class BlockTimeDTO
{
    [Required]
    public DateTime StartDateTime { get; set; }
    
    [Required]
    public DateTime EndDateTime { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Reason { get; set; } = string.Empty;
}
