namespace SecureMedicalRecordSystem.Core.DTOs.Analysis;

public class VitalTrendDto
{
    public string VitalName { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty; // "Improving", "Degrading", "Stable"
    public double Slope { get; set; }
    public double Volatility { get; set; } // standard deviation
    public double? CurrentValue { get; set; }
    public double? BaselineValue { get; set; }
    public double? PercentChangeFromBaseline { get; set; }
    public string HumanInterpretation { get; set; } = string.Empty; // Plain English trend sentence
    public string ActionStep { get; set; } = string.Empty; // Conservative non-diagnostic advice
    public double? NormalMin { get; set; } // Global clinical range
    public double? NormalMax { get; set; } // Global clinical range
    public string? VitalUnit { get; set; } // Unit of measurement (e.g., mg/dL, bpm)
    public string? SectionName { get; set; } // For grouping labs by specialty
    public List<StabilityWindowDto> StabilityWindows { get; set; } = new();
}

public class StabilityWindowDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public double AverageValue { get; set; }
}
