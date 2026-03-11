using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.DTOs.HealthRecords;

public class CreateHealthRecordDTO
{
    [Required]
    public Guid PatientId { get; set; }
    
    public Guid? AppointmentId { get; set; }
    
    public DateTime? RecordDate { get; set; }
    
    [MaxLength(50)]
    public string? RecordType { get; set; }

    // Nested Vitals to match frontend
    public HealthRecordVitalsDTO? Vitals { get; set; }

    // Nested Sections to match frontend
    public List<HealthRecordSectionDTO>? Sections { get; set; }

    // Free Text
    [MaxLength(1000)]
    public string? ChiefComplaint { get; set; }
    
    [MaxLength(5000)]
    public string? DoctorNotes { get; set; }
    
    [MaxLength(1000)]
    public string? Diagnosis { get; set; }
    
    [MaxLength(2000)]
    public string? TreatmentPlan { get; set; }

    // Template tracking
    public Guid? TemplateId { get; set; }
}

public class HealthRecordVitalsDTO
{
    public int BloodPressureSystolic { get; set; }
    public int BloodPressureDiastolic { get; set; }
    public int HeartRate { get; set; }
    public decimal Temperature { get; set; }
    public decimal Weight { get; set; }
    public decimal Height { get; set; }
    public decimal SpO2 { get; set; }
}

public class HealthRecordSectionDTO
{
    public string SectionName { get; set; } = string.Empty;
    public List<HealthRecordAttributeDTO> Attributes { get; set; } = new();
}

public class HealthRecordAttributeDTO
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
