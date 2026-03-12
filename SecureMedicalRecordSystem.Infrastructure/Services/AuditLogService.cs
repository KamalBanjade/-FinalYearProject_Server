using Microsoft.EntityFrameworkCore;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Core.Enums;
using SecureMedicalRecordSystem.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using SecureMedicalRecordSystem.Infrastructure.Data;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;

    private readonly IMemoryCache _cache;
    private const string StatsCacheKey = "AdminDashboardStats";

    public AuditLogService(ApplicationDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
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

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(l =>
                l.Action.Contains(searchTerm) ||
                l.Details.Contains(searchTerm) ||
                (l.User != null && (l.User.Email.Contains(searchTerm) || l.User.FirstName.Contains(searchTerm) || l.User.LastName.Contains(searchTerm))));
        }

        if (!string.IsNullOrEmpty(action))
            query = query.Where(l => l.Action == action);

        if (severity.HasValue)
            query = query.Where(l => l.Severity == severity.Value);

        if (fromDate.HasValue)
            query = query.Where(l => l.Timestamp >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(l => l.Timestamp <= toDate.Value);

        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new SecureMedicalRecordSystem.Core.DTOs.Admin.AuditLogResponseDTO
            {
                Id        = l.Id,
                UserId    = l.UserId,
                UserName  = l.User != null ? $"{l.User.FirstName} {l.User.LastName}" : "System",
                UserEmail = l.User != null ? l.User.Email : null,
                Action    = l.Action,
                Details   = l.Details,
                Timestamp = l.Timestamp,
                IPAddress = l.IPAddress,
                UserAgent = l.UserAgent,
                EntityType = l.EntityType,
                EntityId  = l.EntityId,
                Severity  = l.Severity
            })
            .ToListAsync();

        return (logs, totalCount);
    }

    public async Task<SecureMedicalRecordSystem.Core.DTOs.Admin.SystemStatisticsDTO> GetSystemStatisticsAsync()
    {
        return await _cache.GetOrCreateAsync(StatsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5); // Cache for 5 mins
            
            var now          = DateTime.UtcNow;
            var nowToDay     = now.Date;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var last24h      = now.AddHours(-24);
            var last7d       = nowToDay.AddDays(-6);
            var last4w       = nowToDay.AddDays(-(int)nowToDay.DayOfWeek - 21);

            // 1. Bulk User Statistics
        var userStats = await _context.Users
            .GroupBy(u => 1)
            .Select(g => new {
                Total = g.Count(),
                Active = g.Count(u => u.IsActive),
                NewThisMonth = g.Count(u => u.CreatedAt >= startOfMonth),
                Doctors = _context.Doctors.Count(),
                Patients = _context.Patients.Count()
            })
            .FirstOrDefaultAsync();

        // 2. Grouped Record Statistics (By State)
        var recordStats = await _context.MedicalRecords
            .Where(r => !r.IsDeleted)
            .GroupBy(r => r.State)
            .Select(g => new { State = g.Key, Count = g.Count() })
            .ToListAsync();

        var recordThisMonth = await _context.MedicalRecords
            .CountAsync(r => !r.IsDeleted && r.UploadedAt >= startOfMonth);

        // 3. Grouped QR Access Statistics
        var qrStats = await _context.QRTokens
            .GroupBy(t => t.TokenType)
            .Select(g => new { Type = g.Key, Scans = g.Sum(t => t.AccessCount) })
            .ToListAsync();

        // 4. Appointment Statistics
        var apptStats = await _context.Appointments
            .GroupBy(a => 1)
            .Select(g => new {
                Total = g.Count(),
                Completed = g.Count(a => a.Status == AppointmentStatus.Completed),
                Upcoming = g.Count(a => a.AppointmentDate > now && !a.IsCancelled)
            })
            .FirstOrDefaultAsync();

        // 5. Audit Signals & Recent Critical
        var auditSignals = await _context.AuditLogs
            .GroupBy(l => 1)
            .Select(g => new {
                Total = g.Count(),
                Critical24h = g.Count(l => l.Timestamp >= last24h && l.Severity == AuditSeverity.Critical),
                Warning24h = g.Count(l => l.Timestamp >= last24h && l.Severity == AuditSeverity.Warning)
            })
            .FirstOrDefaultAsync();

        var recentCritical = await _context.AuditLogs
            .Include(l => l.User)
            .Where(l => l.Severity == AuditSeverity.Critical || l.Severity == AuditSeverity.Error)
            .OrderByDescending(l => l.Timestamp)
            .Take(10)
            .Select(l => new SecureMedicalRecordSystem.Core.DTOs.Admin.RecentCriticalEventDTO
            {
                Id        = l.Id,
                Action    = l.Action,
                Details   = l.Details,
                UserName  = l.User != null ? $"{l.User.FirstName} {l.User.LastName}" : "System",
                UserEmail = l.User != null ? l.User.Email : null,
                Timestamp = l.Timestamp,
                Severity  = l.Severity.ToString()
            })
            .ToListAsync();

        // 6. Consolidated Trends
        // QR Trend: Group by Day and Action
        var qrTrendBase = await _context.AuditLogs
            .Where(l => l.Timestamp >= last7d && (l.Action == "EMERGENCY SCAN ACCESS" || l.Action == "DESKTOP SCAN VERIFIED"))
            .GroupBy(l => new { Day = l.Timestamp.Date, l.Action })
            .Select(g => new { g.Key.Day, g.Key.Action, Count = g.Count() })
            .ToListAsync();

        var qrTrend = new List<SecureMedicalRecordSystem.Core.DTOs.Admin.TimeSeriesDataPointDTO>();
        for (int i = 6; i >= 0; i--)
        {
            var day = nowToDay.AddDays(-i);
            qrTrend.Add(new SecureMedicalRecordSystem.Core.DTOs.Admin.TimeSeriesDataPointDTO {
                Label = day.ToString("ddd"),
                Value = qrTrendBase.FirstOrDefault(x => x.Day == day && x.Action == "EMERGENCY SCAN ACCESS")?.Count ?? 0,
                Value2 = qrTrendBase.FirstOrDefault(x => x.Day == day && x.Action == "DESKTOP SCAN VERIFIED")?.Count ?? 0
            });
        }

        // User Growth Trend: Get registration timestamps in bulk
        var userRegistrations = await _context.Users
            .Where(u => u.CreatedAt >= last4w)
            .OrderBy(u => u.CreatedAt)
            .Select(u => new { u.CreatedAt, u.Role })
            .ToListAsync();

        var doctorsFromWindow = await _context.Doctors
            .Where(d => d.User.CreatedAt >= last4w)
            .Select(d => d.User.CreatedAt)
            .ToListAsync();

        var patientsFromWindow = await _context.Patients
            .Where(p => p.User.CreatedAt >= last4w)
            .Select(p => p.User.CreatedAt)
            .ToListAsync();

        var userTrend = new List<SecureMedicalRecordSystem.Core.DTOs.Admin.TimeSeriesDataPointDTO>();
        for (int i = 3; i >= 0; i--)
        {
            var weekStart = nowToDay.AddDays(-(int)nowToDay.DayOfWeek - (i * 7));
            var weekEnd   = weekStart.AddDays(7);
            var label     = i == 0 ? "Today" : weekStart.ToString("MMM dd");
            var cutOff    = (i == 0) ? now : weekEnd;

            // Cumulative Total = Current Snapshot - Count(Created AFTER cutoff)
            userTrend.Add(new SecureMedicalRecordSystem.Core.DTOs.Admin.TimeSeriesDataPointDTO
            {
                Label  = label,
                Value  = (userStats?.Total ?? 0) - userRegistrations.Count(u => u.CreatedAt >= cutOff),
                Value2 = (userStats?.Doctors ?? 0) - doctorsFromWindow.Count(c => c >= cutOff),
                Value3 = (userStats?.Patients ?? 0) - patientsFromWindow.Count(c => c >= cutOff)
            });
        }

            return new SecureMedicalRecordSystem.Core.DTOs.Admin.SystemStatisticsDTO
            {
                TotalUsers               = userStats?.Total ?? 0,
                TotalDoctors             = userStats?.Doctors ?? 0,
                TotalPatients            = userStats?.Patients ?? 0,
                TotalAdmins              = Math.Max(0, (userStats?.Total ?? 0) - (userStats?.Doctors ?? 0) - (userStats?.Patients ?? 0)),
                ActiveUsers              = userStats?.Active ?? 0,
                NewUsersThisMonth        = userStats?.NewThisMonth ?? 0,
                TotalRecordsUploaded     = recordStats.Sum(s => s.Count),
                TotalRecordsDraft        = recordStats.FirstOrDefault(s => s.State == RecordState.Draft)?.Count ?? 0,
                TotalRecordsPending      = recordStats.FirstOrDefault(s => s.State == RecordState.Pending)?.Count ?? 0,
                TotalRecordsCertified    = recordStats.FirstOrDefault(s => s.State == RecordState.Certified)?.Count ?? 0,
                TotalRecordsEmergency    = recordStats.FirstOrDefault(s => s.State == RecordState.Emergency)?.Count ?? 0,
                TotalRecordsArchived     = recordStats.FirstOrDefault(s => s.State == RecordState.Archived)?.Count ?? 0,
                RecordsUploadedThisMonth = recordThisMonth,
                TotalQRScans             = qrStats.Sum(s => s.Scans),
                NormalQRScans            = qrStats.FirstOrDefault(s => s.Type == QRTokenType.Normal)?.Scans ?? 0,
                EmergencyQRScans         = qrStats.FirstOrDefault(s => s.Type == QRTokenType.Emergency)?.Scans ?? 0,
                ActiveAccessSessions     = await _context.AccessSessions.CountAsync(s => s.IsActive && s.ExpiresAt > now),
                TotalAppointments        = apptStats?.Total ?? 0,
                CompletedAppointments    = apptStats?.Completed ?? 0,
                UpcomingAppointments     = apptStats?.Upcoming ?? 0,
                TotalAuditLogs           = auditSignals?.Total ?? 0,
                CriticalEvents24h        = auditSignals?.Critical24h ?? 0,
                WarningEvents24h         = auditSignals?.Warning24h ?? 0,
                RecentCriticalEvents     = recentCritical,
                UserGrowthTrend          = userTrend,
                QRScanTrend              = qrTrend
            };
        });
    }

    public async Task<List<SecureMedicalRecordSystem.Core.DTOs.Admin.SecurityAlertDTO>> GetSecurityAlertsAsync()
    {
        var alerts = new List<SecureMedicalRecordSystem.Core.DTOs.Admin.SecurityAlertDTO>();
        var now    = DateTime.UtcNow;
        var last1h = now.AddHours(-1);
        var last24h= now.AddHours(-24);
        var last7d = now.AddDays(-7);

        // 1. Brute force: >=3 warning-level login events in 1h per user
        var failedLogins = await _context.AuditLogs
            .Where(l => l.Timestamp >= last1h &&
                        (l.Action.Contains("Login") || l.Action.Contains("login")) &&
                        l.Severity == AuditSeverity.Warning)
            .GroupBy(l => l.UserId)
            .Where(g => g.Count() >= 3)
            .Select(g => new
            {
                UserId      = g.Key,
                Count       = g.Count(),
                LastAttempt = g.Max(l => l.Timestamp),
                IPAddress   = g.OrderByDescending(l => l.Timestamp).Select(l => l.IPAddress).FirstOrDefault()
            })
            .ToListAsync();

        foreach (var fl in failedLogins)
        {
            var user = fl.UserId.HasValue ? await _context.Users.FindAsync(fl.UserId.Value) : null;
            alerts.Add(new SecureMedicalRecordSystem.Core.DTOs.Admin.SecurityAlertDTO
            {
                AlertType        = "BRUTE_FORCE",
                Title            = "Suspicious Login Activity",
                Description      = $"Multiple failed login attempts detected ({fl.Count} in 1 hour).",
                Severity         = "High",
                DetectedAt       = fl.LastAttempt,
                RelatedUserId    = fl.UserId,
                RelatedUserEmail = user?.Email,
                RelatedUserName  = user != null ? $"{user.FirstName} {user.LastName}" : null,
                EventCount       = fl.Count,
                IPAddress        = fl.IPAddress
            });
        }

        // 2. Emergency QR overuse: >=3 emergency scans by same doctor in 24h
        var emergencyBursts = await _context.AuditLogs
            .Where(l => l.Timestamp >= last24h && l.Action == "EMERGENCY SCAN ACCESS")
            .GroupBy(l => l.UserId)
            .Where(g => g.Count() >= 3)
            .Select(g => new
            {
                UserId    = g.Key,
                Count     = g.Count(),
                LastEvent = g.Max(l => l.Timestamp)
            })
            .ToListAsync();

        foreach (var eb in emergencyBursts)
        {
            var user = eb.UserId.HasValue ? await _context.Users.FindAsync(eb.UserId.Value) : null;
            alerts.Add(new SecureMedicalRecordSystem.Core.DTOs.Admin.SecurityAlertDTO
            {
                AlertType        = "EMERGENCY_OVERUSE",
                Title            = "High Emergency QR Usage",
                Description      = $"A doctor performed {eb.Count} emergency QR scans in the last 24 hours.",
                Severity         = "Medium",
                DetectedAt       = eb.LastEvent,
                RelatedUserId    = eb.UserId,
                RelatedUserEmail = user?.Email,
                RelatedUserName  = user != null ? $"{user.FirstName} {user.LastName}" : null,
                EventCount       = eb.Count
            });
        }

        // 3. Critical events in the last 24h (max 5, excluding duplicates of above)
        var criticalEvents = await _context.AuditLogs
            .Include(l => l.User)
            .Where(l => l.Timestamp >= last24h &&
                        l.Severity == AuditSeverity.Critical &&
                        l.Action != "EMERGENCY SCAN ACCESS")
            .OrderByDescending(l => l.Timestamp)
            .Take(5)
            .ToListAsync();

        foreach (var ce in criticalEvents)
        {
            alerts.Add(new SecureMedicalRecordSystem.Core.DTOs.Admin.SecurityAlertDTO
            {
                AlertType        = "CRITICAL_EVENT",
                Title            = "Critical System Event",
                Description      = $"{ce.Action}: {ce.Details}",
                Severity         = "Critical",
                DetectedAt       = ce.Timestamp,
                RelatedUserId    = ce.UserId,
                RelatedUserEmail = ce.User?.Email,
                RelatedUserName  = ce.User != null ? $"{ce.User.FirstName} {ce.User.LastName}" : "System",
                EventCount       = 1,
                IPAddress        = ce.IPAddress
            });
        }

        // 4. Off-hours access: >=5 record/QR/access events between midnight–5am in last 7 days
        var offHours = await _context.AuditLogs
            .Where(l => l.Timestamp >= last7d &&
                        (l.Action.Contains("QR") || l.Action.Contains("Access") || l.Action.Contains("Record")) &&
                        l.Timestamp.Hour >= 0 && l.Timestamp.Hour < 5)
            .GroupBy(l => l.UserId)
            .Where(g => g.Count() >= 5)
            .Select(g => new { UserId = g.Key, Count = g.Count(), LastEvent = g.Max(l => l.Timestamp) })
            .ToListAsync();

        foreach (var oh in offHours)
        {
            var user = oh.UserId.HasValue ? await _context.Users.FindAsync(oh.UserId.Value) : null;
            alerts.Add(new SecureMedicalRecordSystem.Core.DTOs.Admin.SecurityAlertDTO
            {
                AlertType        = "OFF_HOURS_ACCESS",
                Title            = "Off-Hours System Access",
                Description      = $"Unusual access pattern: {oh.Count} accesses between midnight and 5 AM in the past 7 days.",
                Severity         = "Low",
                DetectedAt       = oh.LastEvent,
                RelatedUserId    = oh.UserId,
                RelatedUserEmail = user?.Email,
                RelatedUserName  = user != null ? $"{user.FirstName} {user.LastName}" : null,
                EventCount       = oh.Count
            });
        }

        return alerts.OrderByDescending(a => a.DetectedAt).ToList();
    }

    public async Task<int> ApplyRetentionPolicyAsync(int retentionDays)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

        // Critical and Error logs are kept permanently; only purge Info/Warning older than cutoff
        var logsToDelete = await _context.AuditLogs
            .Where(l => l.Timestamp < cutoffDate &&
                        (l.Severity == AuditSeverity.Info || l.Severity == AuditSeverity.Warning))
            .ToListAsync();

        if (logsToDelete.Count > 0)
        {
            _context.AuditLogs.RemoveRange(logsToDelete);
            await _context.SaveChangesAsync();

            await LogAsync(null, "Log Retention Applied",
                $"Purged {logsToDelete.Count} audit logs older than {retentionDays} days (Info/Warning severity only).",
                "0.0.0.0", "System", "AuditLog", null, AuditSeverity.Info);
        }

        return logsToDelete.Count;
    }
}
