namespace SecureMedicalRecordSystem.Core.DTOs.Analysis;

public class StabilityTimelineDto
{
    public List<QuarterlyStabilityDto> Quarters { get; set; } = new();
}

public class QuarterlyStabilityDto
{
    public string Quarter { get; set; } = string.Empty; // e.g. "Q1 2025"
    public double StabilityScore { get; set; } // 0.0 to 100.0
    public int TotalVisits { get; set; }
    public int AbnormalReadingCount { get; set; }
    public bool HasLongGap { get; set; } // true if any gap > 90 days within this quarter
    public string ScoreInterpretation { get; set; } = string.Empty; // "Excellent", "Good", "Fair", "Poor"
}
