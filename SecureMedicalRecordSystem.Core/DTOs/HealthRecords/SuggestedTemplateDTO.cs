namespace SecureMedicalRecordSystem.Core.DTOs.HealthRecords;

public class SuggestedTemplateDTO
{
    public TemplateDTO Template { get; set; } = new();
    public decimal MatchScore { get; set; }
    public string MatchReason { get; set; } = string.Empty;
}
