using SecureMedicalRecordSystem.Core.DTOs.Admin;
using SecureMedicalRecordSystem.Core.DTOs.Doctor;

namespace SecureMedicalRecordSystem.Core.DTOs.Patient;

public class PatientStatisticsDTO
{
    public string FirstName { get; set; } = string.Empty;
    
    // Key Metrics
    public int TotalRecords { get; set; }
    public int CertifiedRecords { get; set; }
    public int PendingRecords { get; set; }
    public int UpcomingAppointments { get; set; }
    
    // Safety & Security
    public bool TotpEnabled { get; set; }
    public int TrustedDevicesCount { get; set; }
    public int ActiveShareCount { get; set; }
    public DateTime? EmergencyDataLastUpdated { get; set; }
    
    // Charts
    public List<TimeSeriesDataPointDTO> RecordTypeDistribution { get; set; } = new();
    public List<RecordGrowthTrendDTO> RecordGrowthTrend { get; set; } = new();
    public List<TimeSeriesDataPointDTO> AppointmentStatusDistribution { get; set; } = new();
    public List<TimeSeriesDataPointDTO> ScanTrend { get; set; } = new();
    public int TotalNormalScans { get; set; }
    public int TotalEmergencyScans { get; set; }
    
    // Activity
    public List<ClinicalActivityDTO> RecentActivities { get; set; } = new();
}
