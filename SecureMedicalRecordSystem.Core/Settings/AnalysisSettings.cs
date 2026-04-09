namespace SecureMedicalRecordSystem.Core.Settings;

public class AnalysisSettings
{
    public int StabilityAlertThreshold { get; set; } = 40;
    public int StabilityWorkerIntervalMinutes { get; set; } = 60;
}
