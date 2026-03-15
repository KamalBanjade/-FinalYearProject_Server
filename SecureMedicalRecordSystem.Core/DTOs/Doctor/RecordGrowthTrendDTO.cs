namespace SecureMedicalRecordSystem.Core.DTOs.Doctor;

public class RecordGrowthTrendDTO
{
    public string Label { get; set; } = string.Empty;
    public string? Resolution { get; set; }
    public int Total { get; set; }
    public int Certified { get; set; }
    public int Pending { get; set; }
    public int Draft { get; set; }
    public int Emergency { get; set; }
    public int Archived { get; set; }
}
