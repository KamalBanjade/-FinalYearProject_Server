using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.Entities;

public class CommonLabUnit : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string MeasurementType { get; set; } = string.Empty;

    [MaxLength(500)]
    public string CommonUnits { get; set; } = "[]"; // JSON array of strings

    [MaxLength(20)]
    public string? DefaultUnit { get; set; }

    public decimal? NormalRangeLow { get; set; }
    public decimal? NormalRangeHigh { get; set; }

    [MaxLength(20)]
    public string? NormalRangeUnit { get; set; }

    [MaxLength(500)]
    public string Aliases { get; set; } = "[]"; // JSON array of strings

    [MaxLength(50)]
    public string? Category { get; set; }
}
