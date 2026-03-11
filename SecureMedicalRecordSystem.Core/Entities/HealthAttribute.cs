using System.ComponentModel.DataAnnotations;
using SecureMedicalRecordSystem.Core.Enums;

namespace SecureMedicalRecordSystem.Core.Entities;

public class HealthAttribute : BaseEntity
{
    [Required]
    public Guid RecordId { get; set; }

    [MaxLength(100)]
    public string? SectionName { get; set; }

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

    // Validation & Ranges
    public decimal? NormalRangeMin { get; set; }
    public decimal? NormalRangeMax { get; set; }
    public bool? IsAbnormal { get; set; }
    public bool IsRequired { get; set; } = false;

    // Display & Ordering
    public int DisplayOrder { get; set; } = 0;
    public bool IsFromTemplate { get; set; } = false;

    // Metadata
    public Guid AddedBy { get; set; }

    // Navigation Properties
    public PatientHealthRecord HealthRecord { get; set; } = null!;
}
