namespace SecureMedicalRecordSystem.Core.DTOs.MedicalRecords;

public class SmartDoctorSuggestionDTO
{
    /// <summary>The top-priority recommended doctor (first non-null from priority chain).</summary>
    public DoctorSuggestionItem? RecommendedDoctor { get; set; }

    /// <summary>Doctor from an upcoming appointment (highest priority).</summary>
    public DoctorSuggestionItem? UpcomingAppointmentDoctor { get; set; }

    /// <summary>Patient's designated primary doctor.</summary>
    public DoctorSuggestionItem? PrimaryDoctor { get; set; }

    /// <summary>Up to 3 doctors from the patient's most recent records.</summary>
    public List<DoctorSuggestionItem> RecentDoctors { get; set; } = [];
}

public class DoctorSuggestionItem
{
    /// <summary>The Doctor entity Id (not UserId) — used as AssignedDoctorId on upload.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>The Identity User GUID string for messaging routing.</summary>
    public string UserId { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;

    /// <summary>"Appointment" | "Primary" | "Recent"</summary>
    public string SuggestionType { get; set; } = string.Empty;

    /// <summary>Human-readable label e.g. "Appointment: Mar 15" or "Last visit 2 days ago"</summary>
    public string? SuggestionLabel { get; set; }
    public string? ProfilePictureUrl { get; set; }
}
