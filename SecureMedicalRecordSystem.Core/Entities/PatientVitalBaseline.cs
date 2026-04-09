namespace SecureMedicalRecordSystem.Core.Entities;

public class PatientVitalBaseline : BaseEntity
{
    public Guid PatientId { get; set; }
    public double? AvgSystolic { get; set; }
    public double? AvgDiastolic { get; set; }
    public double? AvgHeartRate { get; set; }
    public double? AvgBmi { get; set; }
    public double? AvgSpo2 { get; set; }
    public double? AvgTemperature { get; set; }
    public int RecordsUsedForBaseline { get; set; } // how many visits were averaged (max 3)
    public DateTime LastCalculatedAt { get; set; }
}
