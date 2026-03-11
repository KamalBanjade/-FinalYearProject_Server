using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.DTOs.MedicalRecords;

public class UpdateMedicalRecordMetadataDTO
{
    [MaxLength(100)]
    public string? RecordType { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime? RecordDate { get; set; }
    public string? Tags { get; set; }
}
