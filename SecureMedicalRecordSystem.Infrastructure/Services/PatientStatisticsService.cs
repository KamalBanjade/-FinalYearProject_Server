using Microsoft.EntityFrameworkCore;
using SecureMedicalRecordSystem.Core.DTOs.Admin;
using SecureMedicalRecordSystem.Core.DTOs.Doctor;
using SecureMedicalRecordSystem.Core.DTOs.Patient;
using SecureMedicalRecordSystem.Core.Enums;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Infrastructure.Data;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class PatientStatisticsService : IPatientStatisticsService
{
    private readonly ApplicationDbContext _context;

    public PatientStatisticsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PatientStatisticsDTO> GetDashboardStatisticsAsync(Guid userId)
    {
        var patient = await _context.Patients
            .AsNoTracking()
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null) return new PatientStatisticsDTO();

        var patientId = patient.Id;
        var today = DateTime.UtcNow.Date;

        // 1. Fetch minimal projected lists sequentially (replaces ~5 queries + avoids N+1, must be sequential for EF Core)
        var medicalRecords = await _context.MedicalRecords
            .AsNoTracking()
            .Where(r => r.PatientId == patientId)
            .Select(r => new { r.CreatedAt, r.State, r.RecordType })
            .ToListAsync();

        var appointments = await _context.Appointments
            .AsNoTracking()
            .Where(a => a.PatientId == patientId)
            .Select(a => new { a.AppointmentDate, a.Status, a.IsCompleted, a.IsCancelled, a.Duration })
            .ToListAsync();

        var tokens = await _context.QRTokens
            .AsNoTracking()
            .Where(t => t.PatientId == patientId)
            .Select(t => new { t.TokenType, t.AccessCount, t.IsActive, t.ExpiresAt })
            .ToListAsync();

        var scanHistoryData = await _context.ScanHistories
            .AsNoTracking()
            .Where(s => s.PatientId == patientId && s.ScannedAt >= today.AddDays(-6))
            .Select(s => new { s.ScannedAt, s.TokenType })
            .ToListAsync();

        var trustedDevicesCount = await _context.TrustedDevices
            .CountAsync(d => d.UserId == userId && d.IsActive && d.ExpiresAt > DateTime.UtcNow);

        var totalRecords = medicalRecords.Count;
        var certifiedRecords = medicalRecords.Count(r => r.State == RecordState.Certified);
        var pendingRecords = medicalRecords.Count(r => r.State == RecordState.Pending || r.State == RecordState.Draft);
        var upcomingAppointments = appointments.Count(a => a.AppointmentDate >= DateTime.UtcNow && a.Status != AppointmentStatus.Cancelled);
        var activeShares = tokens.Count(q => q.IsActive && q.ExpiresAt > DateTime.UtcNow);

        // 2. Record Type Distribution
        var typeDistribution = medicalRecords
            .GroupBy(r => r.RecordType)
            .Select(g => new TimeSeriesDataPointDTO { Label = g.Key ?? "Other", Value = g.Count() })
            .ToList();

        // 3. Record Growth Trend
        var earliestRecord = medicalRecords.OrderBy(r => r.CreatedAt).FirstOrDefault();
        var growthTrend = new List<RecordGrowthTrendDTO>();

        if (earliestRecord == null)
        {
            growthTrend.Add(new RecordGrowthTrendDTO { Label = today.AddDays(-14).ToString("MMM dd"), Total = 0 });
            growthTrend.Add(new RecordGrowthTrendDTO { Label = today.ToString("MMM dd"), Total = 0 });
        }
        else
        {
            var ageInDays = (DateTime.UtcNow.Date - earliestRecord.CreatedAt.Date).TotalDays;
            int cT = 0, cC = 0, cP = 0, cD = 0, cE = 0, cA = 0;

            if (ageInDays <= 45) // Daily Mode
            {
                var startDate = earliestRecord.CreatedAt.Date;
                var dailyData = medicalRecords
                    .GroupBy(r => r.CreatedAt.Date)
                    .Select(g => new { 
                        Date = g.Key, Total = g.Count(), Cert = g.Count(r => r.State == RecordState.Certified), 
                        Pend = g.Count(r => r.State == RecordState.Pending), Draft = g.Count(r => r.State == RecordState.Draft),
                        Emerg = g.Count(r => r.State == RecordState.Emergency), Arch = g.Count(r => r.State == RecordState.Archived)
                    }).ToList();

                int daysCount = (int)(DateTime.UtcNow.Date - startDate).TotalDays;
                for (int i = 0; i <= daysCount; i++)
                {
                    var d = startDate.AddDays(i);
                    var m = dailyData.FirstOrDefault(x => x.Date == d);
                    if (m != null) { cT += m.Total; cC += m.Cert; cP += m.Pend; cD += m.Draft; cE += m.Emerg; cA += m.Arch; }
                    growthTrend.Add(new RecordGrowthTrendDTO { Label = d.ToString("MMM dd"), Resolution = "Day", Total = cT, Certified = cC, Pending = cP, Draft = cD, Emergency = cE, Archived = cA });
                }
            }
            else if (ageInDays <= 180) // Weekly Mode
            {
                var startDate = earliestRecord.CreatedAt.Date.AddDays(-(int)earliestRecord.CreatedAt.DayOfWeek);
                var sortedRecords = medicalRecords.OrderBy(r => r.CreatedAt).ToList();
                var current = startDate;
                while (current <= DateTime.UtcNow.Date)
                {
                    var next = current.AddDays(7);
                    var weekData = sortedRecords.Where(r => r.CreatedAt >= current && r.CreatedAt < next).ToList();
                    cT += weekData.Count; cC += weekData.Count(r => r.State == RecordState.Certified);
                    cP += weekData.Count(r => r.State == RecordState.Pending); cD += weekData.Count(r => r.State == RecordState.Draft);
                    cE += weekData.Count(r => r.State == RecordState.Emergency); cA += weekData.Count(r => r.State == RecordState.Archived);
                    growthTrend.Add(new RecordGrowthTrendDTO { Label = $"W{GetIso8601WeekOfYear(current)}", Resolution = "Week", Total = cT, Certified = cC, Pending = cP, Draft = cD, Emergency = cE, Archived = cA });
                    current = next;
                }
            }
            else if (ageInDays <= 730) // Monthly Mode
            {
                var startDate = new DateTime(earliestRecord.CreatedAt.Year, earliestRecord.CreatedAt.Month, 1);
                var sortedRecords = medicalRecords.OrderBy(r => r.CreatedAt).ToList();
                var current = startDate;
                while (current <= DateTime.UtcNow.Date)
                {
                    var next = current.AddMonths(1);
                    var monthData = sortedRecords.Where(r => r.CreatedAt >= current && r.CreatedAt < next).ToList();
                    cT += monthData.Count; cC += monthData.Count(r => r.State == RecordState.Certified);
                    cP += monthData.Count(r => r.State == RecordState.Pending); cD += monthData.Count(r => r.State == RecordState.Draft);
                    cE += monthData.Count(r => r.State == RecordState.Emergency); cA += monthData.Count(r => r.State == RecordState.Archived);
                    growthTrend.Add(new RecordGrowthTrendDTO { Label = current.ToString("MMM yy"), Resolution = "Month", Total = cT, Certified = cC, Pending = cP, Draft = cD, Emergency = cE, Archived = cA });
                    current = next;
                }
            }
            else // Yearly Mode
            {
                var startDate = new DateTime(earliestRecord.CreatedAt.Year, 1, 1);
                var sortedRecords = medicalRecords.OrderBy(r => r.CreatedAt).ToList();
                var current = startDate;
                while (current <= DateTime.UtcNow.Date)
                {
                    var next = current.AddYears(1);
                    var yearData = sortedRecords.Where(r => r.CreatedAt >= current && r.CreatedAt < next).ToList();
                    cT += yearData.Count; cC += yearData.Count(r => r.State == RecordState.Certified);
                    cP += yearData.Count(r => r.State == RecordState.Pending); cD += yearData.Count(r => r.State == RecordState.Draft);
                    cE += yearData.Count(r => r.State == RecordState.Emergency); cA += yearData.Count(r => r.State == RecordState.Archived);
                    growthTrend.Add(new RecordGrowthTrendDTO { Label = current.ToString("yyyy"), Resolution = "Year", Total = cT, Certified = cC, Pending = cP, Draft = cD, Emergency = cE, Archived = cA });
                    current = next;
                }
            }
        }

        // 4. Appointment Status Distribution
        var now = DateTime.UtcNow;
        var appointmentStats = appointments
            .Select(a => {
                var status = a.Status;
                if (!a.IsCompleted && !a.IsCancelled && 
                    (status == AppointmentStatus.Scheduled || status == AppointmentStatus.Confirmed || status == AppointmentStatus.InProgress) &&
                    now > a.AppointmentDate.AddMinutes(a.Duration))
                {
                    return "Overdue";
                }
                return status.ToString();
            })
            .GroupBy(s => s)
            .Select(g => new TimeSeriesDataPointDTO { Label = g.Key, Value = g.Count() })
            .ToList();

        // 5. Scan Trend (Last 7 days)
        var scanTrend = new List<TimeSeriesDataPointDTO>();
        for (int i = 6; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            scanTrend.Add(new TimeSeriesDataPointDTO
            {
                Label = date.ToString("ddd"),
                Value = scanHistoryData.Count(x => x.ScannedAt.Date == date && x.TokenType == QRTokenType.Emergency),
                Value2 = scanHistoryData.Count(x => x.ScannedAt.Date == date && x.TokenType == QRTokenType.Normal)
            });
        }

        var totalEmergencyScans = tokens.Where(t => t.TokenType == QRTokenType.Emergency).Sum(t => t.AccessCount);
        var totalNormalScans = tokens.Where(t => t.TokenType == QRTokenType.Normal).Sum(t => t.AccessCount);

        // 6. Recent Activities
        var recentLogs = await _context.AuditLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.Timestamp)
            .Take(5)
            .Select(l => new ClinicalActivityDTO { Id = l.Id, Action = l.Action, Details = l.Details ?? string.Empty, Timestamp = l.Timestamp, Type = "Audit" })
            .ToListAsync();

        var recentScans = await _context.ScanHistories
            .AsNoTracking()
            .Include(s => s.Doctor).ThenInclude(d => d.User)
            .Where(s => s.PatientId == patientId)
            .OrderByDescending(s => s.ScannedAt)
            .Take(5)
            .Select(s => new ClinicalActivityDTO { Id = s.Id, Action = "Security Scan", Details = $"Profile scanned by Dr. {s.Doctor.User.FirstName} {s.Doctor.User.LastName}", Timestamp = s.ScannedAt, Type = "Security" })
            .ToListAsync();

        var combinedActivities = recentLogs.Concat(recentScans)
            .OrderByDescending(a => a.Timestamp)
            .Take(10)
            .ToList();

        return new PatientStatisticsDTO
        {
            FirstName = patient.User?.FirstName ?? "Patient",
            TotalRecords = totalRecords,
            CertifiedRecords = certifiedRecords,
            PendingRecords = pendingRecords,
            UpcomingAppointments = upcomingAppointments,
            TotpEnabled = patient.User?.TwoFactorEnabled ?? false,
            TrustedDevicesCount = trustedDevicesCount,
            ActiveShareCount = activeShares,
            EmergencyDataLastUpdated = patient.EmergencyDataLastUpdated,
            RecordTypeDistribution = typeDistribution,
            RecordGrowthTrend = growthTrend,
            AppointmentStatusDistribution = appointmentStats,
            ScanTrend = scanTrend,
            TotalNormalScans = totalNormalScans,
            TotalEmergencyScans = totalEmergencyScans,
            RecentActivities = combinedActivities
        };
    }

    private static int GetIso8601WeekOfYear(DateTime time)
    {
        // Use ISO 8601 definition of a week
        System.DayOfWeek day = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
        if (day >= System.DayOfWeek.Monday && day <= System.DayOfWeek.Wednesday) { time = time.AddDays(3); }
        return System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, System.Globalization.CalendarWeekRule.FirstFourDayWeek, System.DayOfWeek.Monday);
    }
}
