namespace SecureMedicalRecordSystem.Core.DTOs.QR;

public class EmergencyAccessDTO
{
    public string PatientName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string BloodType { get; set; } = "Unknown";
    public string Allergies { get; set; } = "None listed";
    public string CurrentMedications { get; set; } = "None listed";
    public string ChronicConditions { get; set; } = "None listed";
    public EmergencyContactDTO EmergencyContact { get; set; } = new();
    public string? NotesToResponders { get; set; }
    public DateTime? LastUpdated { get; set; }
    public string Warning { get; set; } = "EMERGENCY ACCESS - Full medical records not visible";
}
