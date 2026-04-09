namespace SecureMedicalRecordSystem.Core.DTOs.Analysis;

public class AnalysisSummaryDto
{
    public Guid PatientId { get; set; }
    public int PatientAge { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? BloodType { get; set; }
    public int TotalVisits { get; set; }
    public DateTime FirstVisit { get; set; }
    public DateTime LastVisit { get; set; }
    public string OverallHealthTrend { get; set; } = string.Empty; // "Improving", "Degrading", "Stable", "Mixed"
    public List<string> KeyInsights { get; set; } = new(); // human-readable bullet points
    public List<string> ActiveMedications { get; set; } = new();
    public List<VitalTrendDto> VitalTrends { get; set; } = new();
    public bool HasMissedFollowUp { get; set; }
    public bool BaselineReliabilityWarning { get; set; }
    public DateTime? NextFollowUpDate { get; set; }
    public List<AttentionItemDto> ItemsNeedingAttention { get; set; } = new();
    public double LatestStabilityScore { get; set; }
}
