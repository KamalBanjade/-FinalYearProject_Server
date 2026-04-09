namespace SecureMedicalRecordSystem.Core.DTOs.Analysis;

public class AttentionItemDto
{
    public string Title { get; set; } = string.Empty;         // e.g. "Blood Pressure Rising"
    public string Description { get; set; } = string.Empty;   // Plain English, 1 sentence
    public string ActionStep { get; set; } = string.Empty;    // e.g. "Discuss with your doctor"
    public string Severity { get; set; } = string.Empty;      // "High" | "Medium" | "Low"
    public string Category { get; set; } = string.Empty;      // "Vital" | "Lab" | "FollowUp" | "Gap" | "Baseline"
}
