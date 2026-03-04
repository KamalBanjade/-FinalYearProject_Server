using Microsoft.EntityFrameworkCore;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Core.Enums;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Infrastructure.Data;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;

    public AuditLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(Guid? userId, string action, string details, string ipAddress, string userAgent, string? entityType = null, string? entityId = null, AuditSeverity severity = AuditSeverity.Info)
    {
        var log = new AuditLog
        {
            UserId = userId,
            Action = action,
            Details = details,
            IPAddress = ipAddress,
            UserAgent = userAgent,
            EntityType = entityType,
            EntityId = entityId,
            Severity = severity,
            Timestamp = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}
