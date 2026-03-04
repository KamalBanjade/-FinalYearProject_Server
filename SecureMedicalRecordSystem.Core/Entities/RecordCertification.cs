namespace SecureMedicalRecordSystem.Core.Entities;

public class RecordCertification : BaseEntity
{
    public Guid RecordId { get; set; }
    public Guid DoctorId { get; set; }

    // Cryptographic Proofs
    public string RecordHash { get; set; } = string.Empty;
    public string DigitalSignature { get; set; } = string.Empty;
    public DateTime SignedAt { get; set; }

    // Optional fields
    public string? CertificationNotes { get; set; }
    public Guid? AppointmentId { get; set; }

    // Status
    public bool IsValid { get; set; } = true;

    // Navigation
    public MedicalRecord MedicalRecord { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
}
