namespace SecureMedicalRecordSystem.Core.Entities;

public class StabilityAlert : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public Guid DoctorId { get; set; }
    public DateTime TriggeredAt { get; set; }
    public string Quarter { get; set; } = string.Empty; // e.g. "Q1 2025"
    public double StabilityScore { get; set; }
    public string ScoreInterpretation { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
}
