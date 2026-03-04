using SecureMedicalRecordSystem.Core.Enums;

namespace SecureMedicalRecordSystem.Core.Entities;

public class MedicalRecord : BaseEntity
{
    public Guid PatientId { get; set; }

    // Doctor Assignment (Optional at upload, required for review)
    public Guid? AssignedDoctorId { get; set; }
    public Doctor? AssignedDoctor { get; set; }

    // Storage
    public string S3ObjectKey { get; set; } = string.Empty;
    public bool IsEncrypted { get; set; } = true;
    public string EncryptionAlgorithm { get; set; } = "AES-256-CBC";

    // File metadata
    public string OriginalFileName { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty; // SHA-256 hash for integrity
    public long FileSizeBytes { get; set; }
    public string MimeType { get; set; } = string.Empty;

    // State
    public RecordState State { get; set; } = RecordState.Draft;

    // Optional Metadata
    public string? RecordType { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public DateTime? RecordDate { get; set; }

    // Versioning
    public int Version { get; set; } = 1;
    public Guid? PreviousVersionId { get; set; }
    public bool IsLatestVersion { get; set; } = true;

    // Soft delete
    public new bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    // Specific Timestamps for Medical Records
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }
    public DateTime? CertifiedAt { get; set; }

    // Navigation
    public Patient Patient { get; set; } = null!;
    public ICollection<RecordCertification> Certifications { get; set; } = new List<RecordCertification>();
}
