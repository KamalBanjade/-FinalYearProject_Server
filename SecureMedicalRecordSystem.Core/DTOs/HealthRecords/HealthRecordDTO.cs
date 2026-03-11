using SecureMedicalRecordSystem.Core.Enums;

namespace SecureMedicalRecordSystem.Core.DTOs.HealthRecords;

public class HealthRecordDTO
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public Guid? AppointmentId { get; set; }
    public DateTime RecordDate { get; set; }
    public string? RecordType { get; set; }

    // Base Vitals (with abnormal flags)
    public string? BloodPressure { get; set; }
    public bool IsBloodPressureAbnormal { get; set; }
    
    public string? HeartRate { get; set; }
    public bool IsHeartRateAbnormal { get; set; }
    
    public string? Temperature { get; set; }
    public bool IsTemperatureAbnormal { get; set; }
    
    public string? Weight { get; set; }
    public bool IsWeightAbnormal { get; set; }
    
    public string? Height { get; set; }
    public bool IsHeightAbnormal { get; set; }
    
    public string? SpO2 { get; set; }
    public bool IsSpO2Abnormal { get; set; }
    
    public decimal? BMI { get; set; }
    public string? BMICategory { get; set; }

    // Free Text
    public string? ChiefComplaint { get; set; }
    public string? DoctorNotes { get; set; }
    public string? Diagnosis { get; set; }
    public string? TreatmentPlan { get; set; }

    // Sections
    public List<SectionDTO> Sections { get; set; } = new();

    // Template Info
    public string? TemplateName { get; set; }
    public Guid? TemplateId { get; set; }

    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? GeneratedPdfUrl { get; set; }
}
