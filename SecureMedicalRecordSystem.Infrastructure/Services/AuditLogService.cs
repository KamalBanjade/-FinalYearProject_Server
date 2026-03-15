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
    private const string DoctorStatsCachePrefix = "DoctorDashboardStats:";

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
        var query = _context.AuditLogs.AsNoTracking();

        // 1. Basic non-join filters first (all indexed)
        if (severity.HasValue)
            query = query.Where(l => l.Severity == severity.Value);

        if (!string.IsNullOrEmpty(action))
            query = query.Where(l => l.Action == action);

        if (fromDate.HasValue)
            query = query.Where(l => l.Timestamp >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(l => l.Timestamp <= toDate.Value);

        // 2. Search term handling (complex queries)
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(l =>
                l.Action.Contains(searchTerm) ||
                l.Details.Contains(searchTerm) ||
                (l.User != null && (l.User.Email.Contains(searchTerm) || 
                                   l.User.FirstName.Contains(searchTerm) || 
                                   l.User.LastName.Contains(searchTerm))));
        }

        // 3. Count optimization (use cache for simple counts)
        int totalCount;
        var isSimpleQuery = string.IsNullOrEmpty(searchTerm) && !fromDate.HasValue && !toDate.HasValue;
        
        if (isSimpleQuery)
        {
            var cacheKey = $"AuditLogCount:{action}:{severity}";
            totalCount = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10);
                return await query.CountAsync();
            });
        }
        else
        {
            totalCount = await query.CountAsync();
        }

        // 4. Data selection with efficient projection
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

    public async Task<SecureMedicalRecordSystem.Core.DTOs.Admin.SystemStatisticsDTO> GetSystemStatisticsAsync(Guid adminUserId)
    {
        var cacheKey = $"{StatsCacheKey}:{adminUserId}";
        return (await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5); // Cache for 5 mins
            
            var adminUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == adminUserId);
            var adminFirstName = adminUser?.FirstName ?? "Admin";

            var now          = DateTime.UtcNow;
            var nowToDay     = now.Date;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var last24h      = now.AddHours(-24);
            var last7d       = nowToDay.AddDays(-6);

            // Calculate dynamic end date (last user activity or registration)
            var lastRegistration = await _context.Users.AsNoTracking().MaxAsync(u => (DateTime?)u.CreatedAt) ?? now;
            var lastLogin        = await _context.Users.AsNoTracking().MaxAsync(u => u.LastLoginAt) ?? now;
            var referenceDate    = lastRegistration > lastLogin ? lastRegistration : lastLogin;
            var referenceDay     = referenceDate.Date;
            
            var last4w = referenceDay.AddDays(-(int)referenceDay.DayOfWeek - 21);

            // 1. Execute queries sequentially to avoid DbContext concurrency issues
            var userStats = await _context.Users
                .OrderBy(u => u.Id)
                .GroupBy(u => 1)
                .Select(g => new {
                    Total = g.Count(),
                    Active = g.Count(u => u.IsActive),
                    NewThisMonth = g.Count(u => u.CreatedAt >= startOfMonth),
                    Doctors = _context.Doctors.Count(),
                    Patients = _context.Patients.Count()
                }).FirstOrDefaultAsync();

            var recordStats = await _context.MedicalRecords
                .Where(r => !r.IsDeleted)
                .GroupBy(r => r.State)
                .Select(g => new { State = g.Key, Count = g.Count() })
                .ToListAsync();

            var recordsThisMonth = await _context.MedicalRecords
                .CountAsync(r => !r.IsDeleted && r.UploadedAt >= startOfMonth);

            var qrStats = await _context.QRTokens
                .GroupBy(t => t.TokenType)
                .Select(g => new { Type = g.Key, Scans = g.Sum(t => t.AccessCount) })
                .ToListAsync();

            var apptStats = await _context.Appointments
                .OrderBy(a => a.Id)
                .GroupBy(a => 1)
                .Select(g => new {
                    Total = g.Count(),
                    Completed = g.Count(a => a.Status == AppointmentStatus.Completed),
                    Upcoming = g.Count(a => a.AppointmentDate > now && !a.IsCancelled)
                }).FirstOrDefaultAsync();

            var auditSignals = await _context.AuditLogs
                .OrderBy(l => l.Id)
                .GroupBy(l => 1)
                .Select(g => new {
                    Total = g.Count(),
                    Critical24h = g.Count(l => l.Timestamp >= last24h && l.Severity == AuditSeverity.Critical),
                    Warning24h = g.Count(l => l.Timestamp >= last24h && l.Severity == AuditSeverity.Warning)
                }).FirstOrDefaultAsync();

            var recentCriticalEvents = await _context.AuditLogs
                .Include(l => l.User)
                .Where(l => l.Severity == AuditSeverity.Critical || l.Severity == AuditSeverity.Error)
                .OrderByDescending(l => l.Timestamp)
                .Take(10)
                .Select(l => new SecureMedicalRecordSystem.Core.DTOs.Admin.RecentCriticalEventDTO
                {
                    Id        = l.Id,
                    Action    = l.Action,
                    Details   = l.Details ?? string.Empty,
                    UserName  = l.User != null ? $"{l.User.FirstName} {l.User.LastName}" : "System",
                    UserEmail = l.User != null ? l.User.Email : null,
                    Timestamp = l.Timestamp,
                    Severity  = l.Severity.ToString()
                }).ToListAsync();

            var qrTrendBase = await _context.ScanHistories
                .Where(s => s.ScannedAt >= last7d)
                .GroupBy(s => new { Day = s.ScannedAt.Date, s.TokenType })
                .Select(g => new { g.Key.Day, g.Key.TokenType, Count = g.Count() })
                .ToListAsync();

            var userRegistrations = await _context.Users
                .Where(u => u.CreatedAt >= last4w)
                .Select(u => new { u.CreatedAt })
                .ToListAsync();

            var doctorsFromWindow = await _context.Doctors
                .Where(d => d.User.CreatedAt >= last4w)
                .Select(d => d.User.CreatedAt)
                .ToListAsync();

            var patientsFromWindow = await _context.Patients
                .Where(p => p.User.CreatedAt >= last4w)
                .Select(p => p.User.CreatedAt)
                .ToListAsync();

            var activeAccessSessions = await _context.AccessSessions.CountAsync(s => s.IsActive && s.ExpiresAt > now);

            var qrTrend = new List<SecureMedicalRecordSystem.Core.DTOs.Admin.TimeSeriesDataPointDTO>();
            for (int i = 6; i >= 0; i--)
            {
                var day = nowToDay.AddDays(-i);
                qrTrend.Add(new SecureMedicalRecordSystem.Core.DTOs.Admin.TimeSeriesDataPointDTO {
                    Label = day.ToString("ddd"),
                    Value = qrTrendBase.Where(x => x.Day == day && x.TokenType == QRTokenType.Emergency).Sum(x => x.Count),
                    Value2 = qrTrendBase.Where(x => x.Day == day && x.TokenType == QRTokenType.Normal).Sum(x => x.Count)
                });
            }

            var userTrend = new List<SecureMedicalRecordSystem.Core.DTOs.Admin.TimeSeriesDataPointDTO>();


            for (int i = 3; i >= 0; i--)
            {
                var weekStart = referenceDay.AddDays(-(int)referenceDay.DayOfWeek - (i * 7));
                var weekEnd   = weekStart.AddDays(7);
                var label     = i == 0 ? (referenceDay == nowToDay ? "Today" : referenceDay.ToString("MMM dd")) : weekStart.ToString("MMM dd");
                var cutOff    = (i == 0) ? referenceDate : weekEnd;

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
                RecordsUploadedThisMonth = recordsThisMonth,
                TotalQRScans             = qrStats.Sum(s => s.Scans),
                NormalQRScans            = qrStats.FirstOrDefault(s => s.Type == QRTokenType.Normal)?.Scans ?? 0,
                EmergencyQRScans         = qrStats.FirstOrDefault(s => s.Type == QRTokenType.Emergency)?.Scans ?? 0,
                ActiveAccessSessions     = activeAccessSessions,
                TotalAppointments        = apptStats?.Total ?? 0,
                CompletedAppointments    = apptStats?.Completed ?? 0,
                UpcomingAppointments     = apptStats?.Upcoming ?? 0,
                TotalAuditLogs           = auditSignals?.Total ?? 0,
                CriticalEvents24h        = auditSignals?.Critical24h ?? 0,
                WarningEvents24h         = auditSignals?.Warning24h ?? 0,
                RecentCriticalEvents     = recentCriticalEvents,
                UserGrowthTrend          = userTrend,
                QRScanTrend              = qrTrend,
                AdminFirstName           = adminFirstName
            };
        }));
    }

    public async Task<SecureMedicalRecordSystem.Core.DTOs.Doctor.DoctorStatisticsDTO> GetDoctorStatisticsAsync(Guid doctorUserId)
    {
        var cacheKey = $"{DoctorStatsCachePrefix}{doctorUserId}";
        return (await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

            var now = DateTime.UtcNow;
            var nowToDay = now.Date;
            var last7d = nowToDay.AddDays(-6);
            
            // 1. Execute queries sequentially
            var recordStats = await _context.MedicalRecords
                .Where(r => r.AssignedDoctorId == doctorUserId && !r.IsDeleted)
                .GroupBy(r => r.State)
                .Select(g => new { State = g.Key, Count = g.Count() })
                .ToListAsync();

            var appointments = await _context.Appointments
                .Where(a => a.DoctorId == doctorUserId && !a.IsCancelled)
                .ToListAsync();

            var recentActions = await _context.AuditLogs
                .Where(l => l.UserId == doctorUserId)
                .OrderByDescending(l => l.Timestamp)
                .Take(10)
                .Select(l => new SecureMedicalRecordSystem.Core.DTOs.Doctor.ClinicalActivityDTO
                {
                    Id = l.Id,
                    Action = l.Action,
                    Details = l.Details ?? string.Empty,
                    Timestamp = l.Timestamp,
                    Type = l.Action.Contains("CERTIFY") ? "Certification" : 
                           l.Action.Contains("REJECT") ? "Rejection" :
                           l.Action.Contains("VIEW") ? "View" : "General"
                })
                .ToListAsync();

            var patientDemographics = await _context.MedicalRecords
                .Where(r => r.AssignedDoctorId == doctorUserId && !r.IsDeleted)
                .Select(r => r.Patient)
                .Select(p => new { p.Gender, p.DateOfBirth })
                .ToListAsync();

            var recordTypeDistribution = await _context.MedicalRecords
                .Where(r => r.AssignedDoctorId == doctorUserId && !r.IsDeleted && r.RecordType != null)
                .GroupBy(r => r.RecordType)
                .Select(g => new SecureMedicalRecordSystem.Core.DTOs.Admin.TimeSeriesDataPointDTO
                {
                    Label = g.Key ?? "Other",
                    Value = g.Count()
                }).ToListAsync();

            var certifiedRecords = await _context.MedicalRecords
                .Where(r => r.AssignedDoctorId == doctorUserId && !r.IsDeleted && 
                            r.State == RecordState.Certified && r.CertifiedAt != null)
                .Select(r => new { r.UploadedAt, r.CertifiedAt })
                .ToListAsync();

            var weeklyAvailability = await _context.DoctorAvailabilities
                .Where(a => a.DoctorId == doctorUserId && a.IsActive)
                .OrderBy(a => a.DayOfWeek)
                .Select(a => new SecureMedicalRecordSystem.Core.DTOs.Doctor.AvailabilitySlotDTO
                {
                    Day = a.DayOfWeek.HasValue 
                        ? ((DayOfWeek)a.DayOfWeek.Value).ToString() 
                        : (a.SpecificDate.HasValue ? a.SpecificDate.Value.ToString("MMM dd") : "N/A"),
                    TimeRange = $"{a.StartTime:hh\\:mm} - {a.EndTime:hh\\:mm}",
                    IsAvailable = a.IsAvailable,
                    Status = a.Reason ?? (a.IsAvailable ? "Active" : "Unavailable")
                }).ToListAsync();

            var clinicalActions24h = await _context.AuditLogs.CountAsync(l => l.UserId == doctorUserId && l.Timestamp >= now.AddHours(-24));

            var totalPatients = patientDemographics.Count;
            var todayCount = appointments.Count(a => a.AppointmentDate.Date == nowToDay);
            var upcomingCount = appointments.Count(a => a.AppointmentDate > now);

            var totalAssigned = recordStats.Sum(s => s.Count);
            var certifiedCount = recordStats.FirstOrDefault(s => s.State == RecordState.Certified)?.Count ?? 0;
            var trustScore = totalAssigned > 0 ? (double)certifiedCount / totalAssigned * 100 : 100.0;

            var apptTrendBase = appointments
                .Where(a => a.AppointmentDate >= last7d && a.AppointmentDate <= now)
                .GroupBy(a => a.AppointmentDate.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToList();

            var apptTrend = new List<SecureMedicalRecordSystem.Core.DTOs.Admin.TimeSeriesDataPointDTO>();
            for (int i = 6; i >= 0; i--)
            {
                var day = nowToDay.AddDays(-i);
                apptTrend.Add(new SecureMedicalRecordSystem.Core.DTOs.Admin.TimeSeriesDataPointDTO
                {
                    Label = day.ToString("ddd"),
                    Value = apptTrendBase.FirstOrDefault(x => x.Day == day)?.Count ?? 0
                });
            }

            var genderDist = patientDemographics
                .GroupBy(p => p.Gender ?? "Unknown")
                .Select(g => new SecureMedicalRecordSystem.Core.DTOs.Admin.TimeSeriesDataPointDTO
                {
                    Label = g.Key,
                    Value = g.Count()
                }).ToList();

            var ageGroups = patientDemographics
                .Select(p => {
                    var age = DateTime.UtcNow.Year - p.DateOfBirth.Year;
                    if (p.DateOfBirth > DateTime.UtcNow.AddYears(-age)) age--;
                    return age;
                })
                .GroupBy(age => age switch {
                    < 18 => "0-17",
                    < 35 => "18-34",
                    < 55 => "35-54",
                    < 75 => "55-74",
                    _ => "75+"
                })
                .OrderBy(g => g.Key)
                .Select(g => new SecureMedicalRecordSystem.Core.DTOs.Admin.TimeSeriesDataPointDTO
                {
                    Label = g.Key,
                    Value = g.Count()
                }).ToList();

            var reasonDist = appointments
                .Where(a => !string.IsNullOrEmpty(a.ReasonForVisit))
                .GroupBy(a => a.ReasonForVisit!)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => new SecureMedicalRecordSystem.Core.DTOs.Admin.TimeSeriesDataPointDTO
                {
                    Label = g.Key,
                    Value = g.Count()
                }).ToList();

            double avgCertTime = certifiedRecords.Count > 0 
                ? certifiedRecords.Average(r => (r.CertifiedAt!.Value - r.UploadedAt).TotalHours) 
                : 0;

            return new SecureMedicalRecordSystem.Core.DTOs.Doctor.DoctorStatisticsDTO
            {
                TotalAssignedPatients = totalPatients,
                PendingRecordReviews = recordStats.FirstOrDefault(s => s.State == RecordState.Pending)?.Count ?? 0,
                TotalCertifiedRecords = certifiedCount,
                TodayAppointments = todayCount,
                UpcomingAppointments = upcomingCount,
                PatientTrustScore = Math.Round(trustScore, 1),
                TotalClinicalActions24h = clinicalActions24h,
                AppointmentTrend = apptTrend,
                RecordStatusDistribution = recordStats.Select(s => new SecureMedicalRecordSystem.Core.DTOs.Admin.TimeSeriesDataPointDTO { Label = s.State.ToString(), Value = s.Count }).ToList(),
                RecordTypeDistribution = recordTypeDistribution,
                PatientGenderDistribution = genderDist,
                PatientAgeGroups = ageGroups,
                AppointmentReasonDistribution = reasonDist,
                WeeklyAvailability = weeklyAvailability,
                AverageCertificationTimeHours = Math.Round(avgCertTime, 1),
                RecentActions = recentActions
            };
        }));
    }

    public async Task<List<SecureMedicalRecordSystem.Core.DTOs.Admin.SecurityAlertDTO>> GetSecurityAlertsAsync()
    {
        var now    = DateTime.UtcNow;
        var last1h = now.AddHours(-1);
        var last24h= now.AddHours(-24);
        var last7d = now.AddDays(-7);

        // 1. Execute queries sequentially
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
            }).ToListAsync();

        var emergencyBursts = await _context.AuditLogs
            .Where(l => l.Timestamp >= last24h && l.Action == "EMERGENCY SCAN ACCESS")
            .GroupBy(l => l.UserId)
            .Where(g => g.Count() >= 3)
            .Select(g => new
            {
                UserId    = g.Key,
                Count     = g.Count(),
                LastEvent = g.Max(l => l.Timestamp)
            }).ToListAsync();

        var criticalEvents = await _context.AuditLogs
            .Include(l => l.User)
            .Where(l => l.Timestamp >= last24h &&
                        l.Severity == AuditSeverity.Critical &&
                        l.Action != "EMERGENCY SCAN ACCESS")
            .OrderByDescending(l => l.Timestamp)
            .Take(5)
            .ToListAsync();

        var offHours = await _context.AuditLogs
            .Where(l => l.Timestamp >= last7d &&
                        (l.Action.Contains("QR") || l.Action.Contains("Access") || l.Action.Contains("Record")) &&
                        l.Timestamp.Hour >= 0 && l.Timestamp.Hour < 5)
            .GroupBy(l => l.UserId)
            .Where(g => g.Count() >= 5)
            .Select(g => new { UserId = g.Key, Count = g.Count(), LastEvent = g.Max(l => l.Timestamp) })
            .ToListAsync();

        // 3. Collect all unique user IDs to fetch them in one bulk query
        var userIdsToFetch = failedLogins.Select(f => f.UserId)
            .Concat(emergencyBursts.Select(e => e.UserId))
            .Concat(offHours.Select(o => o.UserId))
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var usersMap = await _context.Users
            .Where(u => userIdsToFetch.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u);

        var alerts = new List<SecureMedicalRecordSystem.Core.DTOs.Admin.SecurityAlertDTO>();

        // Process Failed Logins
        foreach (var fl in failedLogins)
        {
            usersMap.TryGetValue(fl.UserId ?? Guid.Empty, out var user);
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

        // Process Emergency Bursts
        foreach (var eb in emergencyBursts)
        {
            usersMap.TryGetValue(eb.UserId ?? Guid.Empty, out var user);
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

        // Process Critical Events
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

        // Process Off-Hours Access
        foreach (var oh in offHours)
        {
            usersMap.TryGetValue(oh.UserId ?? Guid.Empty, out var user);
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
