namespace SecureMedicalRecordSystem.Core.Entities;

/// <summary>
/// Uploaded files (PDFs, images, lab reports) attached to a medical record.
/// File content is AES-256 encrypted on disk.
/// </summary>
public class MedicalFile : BaseEntity
{
    public Guid MedicalRecordId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;  // GUID-based, no PII
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string EncryptionKeyRef { get; set; } = string.Empty;  // Key identifier, not the key itself
    public string? FileHash { get; set; }  // SHA-256 integrity check

    // Navigation
    public MedicalRecord MedicalRecord { get; set; } = null!;
}
