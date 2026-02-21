using SecureMedicalRecordSystem.Core.Enums;

namespace SecureMedicalRecordSystem.Core.Entities;

/// <summary>
/// A single medical encounter / record entry, AES-256 encrypted.
/// </summary>
public class MedicalRecord : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;

    // Encrypted fields (stored as ciphertext)
    public string EncryptedDiagnosis { get; set; } = string.Empty;
    public string EncryptedNotes { get; set; } = string.Empty;
    public string EncryptedPrescriptions { get; set; } = string.Empty;
    public string? EncryptedLabResults { get; set; }

    // Metadata (not encrypted)
    public string RecordType { get; set; } = string.Empty;  // e.g., "Consultation", "Lab", "Imaging"
    public DateTime VisitDate { get; set; }
    public string? IcdCode { get; set; }  // International Classification of Diseases

    // Digital signature (RSA-2048)
    public string? DigitalSignature { get; set; }
    public DateTime? SignedAt { get; set; }

    // Navigation
    public Patient Patient { get; set; } = null!;
    public User Doctor { get; set; } = null!;
    public ICollection<MedicalFile> Files { get; set; } = new List<MedicalFile>();
}
