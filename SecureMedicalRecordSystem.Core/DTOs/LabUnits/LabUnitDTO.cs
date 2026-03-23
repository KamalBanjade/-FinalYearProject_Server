namespace SecureMedicalRecordSystem.Core.DTOs.LabUnits;

public class LabUnitDTO
{
    public Guid Id { get; set; }
    public string MeasurementType { get; set; } = string.Empty;
    public List<string> CommonUnits { get; set; } = new();
    public string? DefaultUnit { get; set; }
    public decimal? NormalRangeLow { get; set; }
    public decimal? NormalRangeHigh { get; set; }
    public string? NormalRangeUnit { get; set; }
    public List<string> Aliases { get; set; } = new();
    public string? Category { get; set; }
}

public class CreateCustomLabUnitDTO
{
    public string MeasurementType { get; set; } = string.Empty;
    public List<string> CommonUnits { get; set; } = new();
    public string? DefaultUnit { get; set; }
    public decimal? NormalRangeLow { get; set; }
    public decimal? NormalRangeHigh { get; set; }
    public string? NormalRangeUnit { get; set; }
    public string? Category { get; set; }
}
