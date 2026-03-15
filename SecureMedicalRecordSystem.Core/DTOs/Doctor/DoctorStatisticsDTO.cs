using SecureMedicalRecordSystem.Core.DTOs.Admin;

namespace SecureMedicalRecordSystem.Core.DTOs.Doctor;

public class DoctorStatisticsDTO
{
    // Key Metrics
    public int TotalAssignedPatients { get; set; }
    public int PendingRecordReviews { get; set; }
    public int TotalCertifiedRecords { get; set; }
    public int TodayAppointments { get; set; }
    public int UpcomingAppointments { get; set; }
    
    // Performance/Engagement
    public double PatientTrustScore { get; set; } // 0-100% based on certifications
    public int TotalClinicalActions24h { get; set; }

    // Charts
    public List<TimeSeriesDataPointDTO> AppointmentTrend { get; set; } = new();
    public List<TimeSeriesDataPointDTO> RecordStatusDistribution { get; set; } = new();
    public List<TimeSeriesDataPointDTO> PatientGenderDistribution { get; set; } = new();
    public List<TimeSeriesDataPointDTO> PatientAgeGroups { get; set; } = new();
    public List<TimeSeriesDataPointDTO> RecordTypeDistribution { get; set; } = new();
    public List<TimeSeriesDataPointDTO> AppointmentReasonDistribution { get; set; } = new();
    
    // Availability & Efficiency
    public List<AvailabilitySlotDTO> WeeklyAvailability { get; set; } = new();
    public double AverageCertificationTimeHours { get; set; }
    
    // Activity
    public List<ClinicalActivityDTO> RecentActions { get; set; } = new();
}

public class AvailabilitySlotDTO
{
    public string Day { get; set; } = string.Empty;
    public string TimeRange { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public string? Status { get; set; }
}

public class ClinicalActivityDTO
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string? PatientName { get; set; }
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } = string.Empty; // e.g. "Certification", "Appointment", "RecordView"
}
