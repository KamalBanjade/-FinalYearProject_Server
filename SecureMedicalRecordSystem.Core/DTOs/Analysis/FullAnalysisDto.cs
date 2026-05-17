using System;
using System.Collections.Generic;

namespace SecureMedicalRecordSystem.Core.DTOs.Analysis;

public class FullAnalysisDto
{
    public AnalysisSummaryDto Summary { get; set; } = null!;
    public List<VitalTrendDto> Trends { get; set; } = new();
    public List<MedicationCorrelationDto> Correlations { get; set; } = new();
    public List<AbnormalityPatternDto> Patterns { get; set; } = new();
    public StabilityTimelineDto Timeline { get; set; } = null!;
}
