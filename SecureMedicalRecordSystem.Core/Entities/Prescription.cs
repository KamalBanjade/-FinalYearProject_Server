namespace SecureMedicalRecordSystem.Core.Entities;

public class Prescription : BaseEntity
{
    public Guid PatientHealthRecordId { get; set; }
    public PatientHealthRecord PatientHealthRecord { get; set; } = null!;
    public string MedicationName { get; set; } = string.Empty;
    public string? Dosage { get; set; }
    public string? Frequency { get; set; }
    public string? Notes { get; set; }
}
