using SecureMedicalRecordSystem.Core.DTOs.Analysis;

namespace SecureMedicalRecordSystem.Core.Interfaces;

public interface INotificationService
{
    Task SendStabilityAlertAsync(Guid doctorId, StabilityAlertDto alert);
}
