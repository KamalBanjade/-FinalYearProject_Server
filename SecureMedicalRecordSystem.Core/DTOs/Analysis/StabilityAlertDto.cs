namespace SecureMedicalRecordSystem.Core.DTOs.Analysis;

public class StabilityAlertDto
{
    public Guid AlertId { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Quarter { get; set; } = string.Empty;
    public double StabilityScore { get; set; }
    public string ScoreInterpretation { get; set; } = string.Empty;
    public DateTime TriggeredAt { get; set; }
    public bool IsRead { get; set; }
}
