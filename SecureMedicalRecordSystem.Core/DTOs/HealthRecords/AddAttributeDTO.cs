using System.ComponentModel.DataAnnotations;
using SecureMedicalRecordSystem.Core.Enums;

namespace SecureMedicalRecordSystem.Core.DTOs.HealthRecords;

public class AddAttributeDTO
{
    [Required]
    [MaxLength(100)]
    public string SectionName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FieldName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string FieldLabel { get; set; } = string.Empty;

    [Required]
    public FieldType FieldType { get; set; }

    [Required]
    public string FieldValue { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? FieldUnit { get; set; }

    public decimal? NormalRangeMin { get; set; }
    public decimal? NormalRangeMax { get; set; }
    
    public int DisplayOrder { get; set; }
}
