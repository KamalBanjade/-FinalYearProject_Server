using SecureMedicalRecordSystem.Core.DTOs.Patient;

namespace SecureMedicalRecordSystem.Core.Interfaces;

public interface IPatientStatisticsService
{
    Task<PatientStatisticsDTO> GetDashboardStatisticsAsync(Guid userId);
}
