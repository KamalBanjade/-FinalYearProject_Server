namespace SecureMedicalRecordSystem.Core.DTOs.Appointments;

public class FollowUpAppointmentResult
{
    public bool WasScheduled { get; set; }
    public Guid? NewAppointmentId { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public string? Message { get; set; }
}

public class ScheduleFollowUpRequest
{
    public Guid? OriginalAppointmentId { get; set; }
    public Guid? PatientId { get; set; }           // Required if OriginalAppointmentId is null
    public DateTime PreferredFollowUpDate { get; set; } // Full ISO date/time from frontend
}
