using SecureMedicalRecordSystem.Core.Enums;

namespace SecureMedicalRecordSystem.Core.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(Guid? userId, string action, string details, string ipAddress, string userAgent, string? entityType = null, string? entityId = null, AuditSeverity severity = AuditSeverity.Info);
}
