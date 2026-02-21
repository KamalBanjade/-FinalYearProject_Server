namespace SecureMedicalRecordSystem.Core.Entities;

/// <summary>
/// Patient demographic and profile information.
/// </summary>
public class Patient : BaseEntity
{
    public string NationalId { get; set; } = string.Empty; // Encrypted at rest
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? Address { get; set; }  // Encrypted at rest
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? BloodType { get; set; }
    public string? Allergies { get; set; }

    // QR Code for quick access
    public string? QrCodeData { get; set; }
    public string? QrCodeImagePath { get; set; }
    public Guid? UserId { get; set; }  // Patient's portal account

    // Navigation
    public User? User { get; set; }
    public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
}
