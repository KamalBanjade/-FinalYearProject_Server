using System;

namespace SecureMedicalRecordSystem.Core.DTOs.HealthRecords;

public class VitalSignsDTO
{
    public int? BloodPressureSystolic { get; set; }
    public int? BloodPressureDiastolic { get; set; }
    public int? HeartRate { get; set; }
    public decimal? Temperature { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Height { get; set; }
    public int? SpO2 { get; set; }
    public DateTime RecordedAt { get; set; }
}

public class VitalsComparisonDTO
{
    public VitalSignsDTO? LastVisit { get; set; }
    public VitalSignsDTO? Suggested { get; set; }
    public List<string> LockedFields { get; set; } = new();
}
