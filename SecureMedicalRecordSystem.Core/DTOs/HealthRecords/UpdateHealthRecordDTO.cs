using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.DTOs.HealthRecords;

public class UpdateHealthRecordDTO
{
    [MaxLength(50)]
    public string? RecordType { get; set; }

    // Base Vitals
    public int? BloodPressureSystolic { get; set; }
    public int? BloodPressureDiastolic { get; set; }
    public int? HeartRate { get; set; }
    public decimal? Temperature { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Height { get; set; }
    public decimal? SpO2 { get; set; }

    // Free Text
    [MaxLength(1000)]
    public string? ChiefComplaint { get; set; }
    
    [MaxLength(5000)]
    public string? DoctorNotes { get; set; }
    
    [MaxLength(1000)]
    public string? Diagnosis { get; set; }
    
    [MaxLength(2000)]
    public string? TreatmentPlan { get; set; }

    // Updated or newly added template values
    public Dictionary<string, string>? AttributeValues { get; set; }

    // Ad-hoc fields added that aren't in the template
    public List<AddAttributeDTO>? NewCustomAttributes { get; set; }
    
    // IDs of attributes to remove
    public List<Guid>? AttributesToRemove { get; set; }
}
