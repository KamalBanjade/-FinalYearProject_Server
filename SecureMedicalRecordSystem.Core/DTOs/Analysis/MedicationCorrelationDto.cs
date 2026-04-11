namespace SecureMedicalRecordSystem.Core.DTOs.Analysis;

public class MedicationCorrelationDto
{
    public string MedicationName { get; set; } = string.Empty;
    public DateTime IntroducedAt { get; set; }
    public DateTime LastSeenAt { get; set; } // Last visit this medication appeared in
    public bool IsCurrentlyActive { get; set; } // True if present in most recent record
    public string DrugCategory { get; set; } = "General";
    public List<string> PrimaryMarkers { get; set; } = new();
    public List<VitalCorrelationDeltaDto> VitalDeltas { get; set; } = new();
}

public class VitalCorrelationDeltaDto
{
    public string VitalName { get; set; } = string.Empty;
    public double AvgBefore { get; set; }
    public double AvgAfter { get; set; }
    public double Delta { get; set; } // positive = increased, negative = decreased
    public string Interpretation { get; set; } = string.Empty; // "Improved", "Degraded", "Neutral"
    public int VisitsBeforeCount { get; set; }
    public int VisitsAfterCount { get; set; }
}
