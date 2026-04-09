using SecureMedicalRecordSystem.Core.DTOs.Analysis;

namespace SecureMedicalRecordSystem.Core.Interfaces;

public interface IHealthAnalysisService
{
    Task<List<VitalTrendDto>> GetVitalTrendsAsync(Guid patientId);
    Task<List<MedicationCorrelationDto>> GetMedicationCorrelationsAsync(Guid patientId);
    Task<List<AbnormalityPatternDto>> GetAbnormalityPatternsAsync(Guid patientId);
    Task<StabilityTimelineDto> GetStabilityTimelineAsync(Guid patientId);
    Task<AnalysisSummaryDto> GetAnalysisSummaryAsync(Guid patientId);
}
