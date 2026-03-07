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

    public async Task<(List<SecureMedicalRecordSystem.Core.DTOs.Admin.AuditLogResponseDTO> Logs, int TotalCount)> GetLogsAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        string? action = null,
        AuditSeverity? severity = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = _context.AuditLogs
            .Include(l => l.User)
            .AsQueryable();

        // Filters
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(l => 
                l.Action.Contains(searchTerm) || 
                l.Details.Contains(searchTerm) || 
                (l.User != null && (l.User.Email.Contains(searchTerm) || l.User.FirstName.Contains(searchTerm) || l.User.LastName.Contains(searchTerm))));
        }

        if (!string.IsNullOrEmpty(action))
        {
            query = query.Where(l => l.Action == action);
        }

        if (severity.HasValue)
        {
            query = query.Where(l => l.Severity == severity.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(l => l.Timestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(l => l.Timestamp <= toDate.Value);
        }

        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new SecureMedicalRecordSystem.Core.DTOs.Admin.AuditLogResponseDTO
            {
                Id = l.Id,
                UserId = l.UserId,
                UserName = l.User != null ? $"{l.User.FirstName} {l.User.LastName}" : "System",
                UserEmail = l.User != null ? l.User.Email : null,
                Action = l.Action,
                Details = l.Details,
                Timestamp = l.Timestamp,
                IPAddress = l.IPAddress,
                UserAgent = l.UserAgent,
                EntityType = l.EntityType,
                EntityId = l.EntityId,
                Severity = l.Severity
            })
            .ToListAsync();

        return (logs, totalCount);
    }
}
