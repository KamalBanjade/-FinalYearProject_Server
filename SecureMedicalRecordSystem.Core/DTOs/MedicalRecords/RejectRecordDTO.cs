using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.DTOs.MedicalRecords;

public class RejectRecordDTO
{
    [Required]
    [MaxLength(500)]
    public string RejectionReason { get; set; } = string.Empty;
}
