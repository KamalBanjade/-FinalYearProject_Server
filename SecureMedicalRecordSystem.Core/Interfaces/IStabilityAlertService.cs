using SecureMedicalRecordSystem.Core.DTOs.Analysis;

namespace SecureMedicalRecordSystem.Core.Interfaces;

public interface IStabilityAlertService
{
    Task CheckAndTriggerAlertsAsync(CancellationToken cancellationToken);
    Task<List<StabilityAlertDto>> GetUnreadAlertsForDoctorAsync(Guid doctorId);
    Task<List<StabilityAlertDto>> GetRecentAlertsForDoctorAsync(Guid doctorId, int count = 20);
    Task MarkAlertAsReadAsync(Guid alertId, Guid doctorId);
    Task<int> GetUnreadAlertCountAsync(Guid doctorId);
}
