namespace SecureMedicalRecordSystem.Core.DTOs;

public class UpdatePatientProfileDTO
{
    public string? BloodType { get; set; }
    public string? Address { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? Allergies { get; set; }
    public string? ChronicConditions { get; set; }
    public string? Occupation { get; set; }
    public string? Gender { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
}
