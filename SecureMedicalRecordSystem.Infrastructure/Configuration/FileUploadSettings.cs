namespace SecureMedicalRecordSystem.Infrastructure.Configuration;

/// <summary>
/// Settings for file upload validation and storage paths.
/// </summary>
public class FileUploadSettings
{
    public int MaxFileSizeMB { get; set; } = 10;
    public List<string> AllowedExtensions { get; set; } = new() { ".pdf", ".jpg", ".jpeg", ".png", ".dcm" };
    public List<string> AllowedMimeTypes { get; set; } = new()
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "application/dicom"
    };
    public string StoragePath { get; set; } = string.Empty;
    public bool EncryptionEnabled { get; set; } = true;

    public long MaxFileSizeBytes => (long)MaxFileSizeMB * 1024 * 1024;
}
