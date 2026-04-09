namespace SecureMedicalRecordSystem.Core.Entities;

public class AnalysisReport : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid GeneratedByDoctorId { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string TigrisObjectKey { get; set; } = string.Empty; // encrypted .enc file key
    public string ReportTitle { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
}
