namespace SecureMedicalRecordSystem.Core.DTOs.QR;

public class UpdateEmergencySettingsDTO
{
    public string? BloodType { get; set; }
    public string? Allergies { get; set; }
    public string? CurrentMedications { get; set; }
    public string? ChronicConditions { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public string? EmergencyNotesToResponders { get; set; }
}
