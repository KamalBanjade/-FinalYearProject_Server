using SecureMedicalRecordSystem.Core.Enums;

namespace SecureMedicalRecordSystem.Core.DTOs.MedicalRecords;

public class MedicalRecordResponseDTO
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string? RecordType { get; set; }
    public string? Description { get; set; }
    public DateTime? RecordDate { get; set; }
    public long FileSize { get; set; }
    public string FileSizeFormatted { get; set; } = string.Empty; // e.g., "2.5 MB"
    public string MimeType { get; set; } = string.Empty;
    public RecordState State { get; set; }
    public string StateLabel { get; set; } = string.Empty; // Human-readable state
    public string? RejectionReason { get; set; }
    public DateTime UploadedAt { get; set; }
    public string UploadedBy { get; set; } = string.Empty; // Uploader name
    public string PatientName { get; set; } = string.Empty;
    
    // Assignment Info
    public string? AssignedDoctorName { get; set; }
    public string? AssignedDepartment { get; set; }

    // Certification Info
    public bool IsCertified { get; set; }
    public string? CertifiedBy { get; set; } // Doctor name
    public DateTime? CertifiedAt { get; set; }
    
    public int Version { get; set; }
    public bool CanDownload { get; set; } // Based on user permissions
}
