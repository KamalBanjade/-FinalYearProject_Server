using SecureMedicalRecordSystem.Core.Enums;

namespace SecureMedicalRecordSystem.Core.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(Guid? userId, string action, string details, string ipAddress, string userAgent, string? entityType = null, string? entityId = null, AuditSeverity severity = AuditSeverity.Info);
    Task<(List<SecureMedicalRecordSystem.Core.DTOs.Admin.AuditLogResponseDTO> Logs, int TotalCount)> GetLogsAsync(
        int page, 
        int pageSize, 
        string? searchTerm = null, 
        string? action = null, 
        SecureMedicalRecordSystem.Core.Enums.AuditSeverity? severity = null, 
        DateTime? fromDate = null, 
        DateTime? toDate = null);
}
