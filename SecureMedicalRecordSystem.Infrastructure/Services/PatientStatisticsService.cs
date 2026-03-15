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
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null) return new PatientStatisticsDTO();

        var patientId = patient.Id;
        var today = DateTime.UtcNow.Date;

        // 1. Basic Stats
        var totalRecords = await _context.MedicalRecords.CountAsync(r => r.PatientId == patientId);
        var certifiedRecords = await _context.MedicalRecords.CountAsync(r => r.PatientId == patientId && r.State == RecordState.Certified);
        var pendingRecords = await _context.MedicalRecords.CountAsync(r => r.PatientId == patientId && (r.State == RecordState.Pending || r.State == RecordState.Draft));
        var upcomingAppointments = await _context.Appointments.CountAsync(a => a.PatientId == patientId && a.AppointmentDate >= DateTime.UtcNow && a.Status != AppointmentStatus.Cancelled);

        // Security Stats
        var trustedDevicesCount = await _context.TrustedDevices.CountAsync(d => d.UserId == userId && d.IsActive && d.ExpiresAt > DateTime.UtcNow);
        var activeShares = await _context.QRTokens.CountAsync(q => q.PatientId == patientId && q.IsActive && q.ExpiresAt > DateTime.UtcNow);

        // 2. Record Type Distribution
        var typeDistribution = await _context.MedicalRecords
            .Where(r => r.PatientId == patientId)
            .GroupBy(r => r.RecordType)
            .Select(g => new TimeSeriesDataPointDTO
            {
                Label = g.Key ?? "Other",
                Value = (int)g.Count()
            })
            .ToListAsync();

        // 3. Record Growth Trend
        var earliestRecord = await _context.MedicalRecords
            .Where(r => r.PatientId == patientId)
            .OrderBy(r => r.CreatedAt)
            .Select(r => (DateTime?)r.CreatedAt)
            .FirstOrDefaultAsync();

        var growthTrend = new List<RecordGrowthTrendDTO>();
        if (earliestRecord == null)
        {
            growthTrend.Add(new RecordGrowthTrendDTO { Label = today.AddDays(-14).ToString("MMM dd"), Total = 0 });
            growthTrend.Add(new RecordGrowthTrendDTO { Label = today.ToString("MMM dd"), Total = 0 });
        }
        else
        {
            var ageInDays = (DateTime.UtcNow.Date - earliestRecord.Value.Date).TotalDays;
            
            if (ageInDays <= 45) // Daily Mode (Show last 15 days as requested)
            {
                var startDate = DateTime.UtcNow.Date.AddDays(-14);
                var dailyData = await _context.MedicalRecords
                    .Where(r => r.PatientId == patientId && r.CreatedAt >= startDate)
                    .GroupBy(r => r.CreatedAt.Date)
                    .Select(g => new { 
                        Date = g.Key, 
                        Total = g.Count(), 
                        Cert = g.Count(r => r.State == RecordState.Certified), 
                        Pend = g.Count(r => r.State == RecordState.Pending), 
                        Draft = g.Count(r => r.State == RecordState.Draft),
                        Emerg = g.Count(r => r.State == RecordState.Emergency),
                        Arch = g.Count(r => r.State == RecordState.Archived)
                    })
                    .ToListAsync();

                int cT = 0, cC = 0, cP = 0, cD = 0, cE = 0, cA = 0;
                for (int i = 0; i < 15; i++)
                {
                    var d = startDate.AddDays(i);
                    var m = dailyData.FirstOrDefault(x => x.Date == d);
                    if (m != null) { cT += m.Total; cC += m.Cert; cP += m.Pend; cD += m.Draft; cE += m.Emerg; cA += m.Arch; }
                    growthTrend.Add(new RecordGrowthTrendDTO { Label = d.ToString("MMM dd"), Resolution = "Day", Total = cT, Certified = cC, Pending = cP, Draft = cD, Emergency = cE, Archived = cA });
                }
            }
            else if (ageInDays <= 180) // Weekly Mode
            {
                var startDate = earliestRecord.Value.Date.AddDays(-(int)earliestRecord.Value.DayOfWeek); // Start of week
                var records = await _context.MedicalRecords
                    .Where(r => r.PatientId == patientId)
                    .OrderBy(r => r.CreatedAt)
                    .ToListAsync();

                int cT = 0, cC = 0, cP = 0, cD = 0, cE = 0, cA = 0;
                var current = startDate;
                while (current <= DateTime.UtcNow.Date)
                {
                    var next = current.AddDays(7);
                    var weekData = records.Where(r => r.CreatedAt >= current && r.CreatedAt < next);
                    cT += weekData.Count();
                    cC += weekData.Count(r => r.State == RecordState.Certified);
                    cP += weekData.Count(r => r.State == RecordState.Pending);
                    cD += weekData.Count(r => r.State == RecordState.Draft);
                    cE += weekData.Count(r => r.State == RecordState.Emergency);
                    cA += weekData.Count(r => r.State == RecordState.Archived);

                    growthTrend.Add(new RecordGrowthTrendDTO { Label = $"W{GetIso8601WeekOfYear(current)}", Resolution = "Week", Total = cT, Certified = cC, Pending = cP, Draft = cD, Emergency = cE, Archived = cA });
                    current = next;
                }
            }
            else if (ageInDays <= 730) // Monthly Mode
            {
                var startDate = new DateTime(earliestRecord.Value.Year, earliestRecord.Value.Month, 1);
                var records = await _context.MedicalRecords
                    .Where(r => r.PatientId == patientId)
                    .OrderBy(r => r.CreatedAt)
                    .ToListAsync();

                int cT = 0, cC = 0, cP = 0, cD = 0, cE = 0, cA = 0;
                var current = startDate;
                while (current <= DateTime.UtcNow.Date)
                {
                    var next = current.AddMonths(1);
                    var monthData = records.Where(r => r.CreatedAt >= current && r.CreatedAt < next);
                    cT += monthData.Count();
                    cC += monthData.Count(r => r.State == RecordState.Certified);
                    cP += monthData.Count(r => r.State == RecordState.Pending);
                    cD += monthData.Count(r => r.State == RecordState.Draft);
                    cE += monthData.Count(r => r.State == RecordState.Emergency);
                    cA += monthData.Count(r => r.State == RecordState.Archived);

                    growthTrend.Add(new RecordGrowthTrendDTO { Label = current.ToString("MMM yy"), Resolution = "Month", Total = cT, Certified = cC, Pending = cP, Draft = cD, Emergency = cE, Archived = cA });
                    current = next;
                }
            }
            else // Yearly Mode
            {
                var startDate = new DateTime(earliestRecord.Value.Year, 1, 1);
                var records = await _context.MedicalRecords
                    .Where(r => r.PatientId == patientId)
                    .OrderBy(r => r.CreatedAt)
                    .ToListAsync();

                int cT = 0, cC = 0, cP = 0, cD = 0, cE = 0, cA = 0;
                var current = startDate;
                while (current <= DateTime.UtcNow.Date)
                {
                    var next = current.AddYears(1);
                    var yearData = records.Where(r => r.CreatedAt >= current && r.CreatedAt < next);
                    cT += yearData.Count();
                    cC += yearData.Count(r => r.State == RecordState.Certified);
                    cP += yearData.Count(r => r.State == RecordState.Pending);
                    cD += yearData.Count(r => r.State == RecordState.Draft);
                    cE += yearData.Count(r => r.State == RecordState.Emergency);
                    cA += yearData.Count(r => r.State == RecordState.Archived);

                    growthTrend.Add(new RecordGrowthTrendDTO { Label = current.ToString("yyyy"), Resolution = "Year", Total = cT, Certified = cC, Pending = cP, Draft = cD, Emergency = cE, Archived = cA });
                    current = next;
                }
            }
        }

        // 4. Appointment Status Distribution
        var now = DateTime.UtcNow;
        var appointments = await _context.Appointments
            .Where(a => a.PatientId == patientId)
            .ToListAsync();

        var appointmentStats = appointments
            .Select(a => {
                var status = a.Status;
                // Dynamic status transition: Scheduled/Confirmed/InProgress -> Overdue if time exceeded
                if (!a.IsCompleted && !a.IsCancelled && 
                    (status == AppointmentStatus.Scheduled || 
                     status == AppointmentStatus.Confirmed || 
                     status == AppointmentStatus.InProgress) &&
                    now > a.AppointmentDate.AddMinutes(a.Duration))
                {
                    return "Overdue";
                }
                return status.ToString();
            })
            .GroupBy(s => s)
            .Select(g => new TimeSeriesDataPointDTO
            {
                Label = g.Key,
                Value = (int)g.Count()
            })
            .ToList();

        // 5. Scan Trend (Last 7 days, Emergency vs Normal)
        // Using ScanHistory as the primary source of truth for scans
        var scanHistoryData = await _context.ScanHistories
            .Where(s => s.PatientId == patientId && s.ScannedAt >= today.AddDays(-6))
            .GroupBy(s => new { Day = s.ScannedAt.Date, s.TokenType })
            .Select(g => new { g.Key.Day, g.Key.TokenType, Count = g.Count() })
            .ToListAsync();
        
        var scanTrend = new List<TimeSeriesDataPointDTO>();
        for (int i = 6; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            scanTrend.Add(new TimeSeriesDataPointDTO
            {
                Label = date.ToString("ddd"),
                Value = scanHistoryData.Where(x => x.Day == date && x.TokenType == QRTokenType.Emergency).Sum(x => x.Count),
                Value2 = scanHistoryData.Where(x => x.Day == date && x.TokenType == QRTokenType.Normal).Sum(x => x.Count)
            });
        }

        // Aggregate counts from tokens to match management table
        var totalEmergencyScans = await _context.QRTokens
            .Where(t => t.PatientId == patientId && t.TokenType == QRTokenType.Emergency)
            .SumAsync(t => t.AccessCount);
        
        var totalNormalScans = await _context.QRTokens
            .Where(t => t.PatientId == patientId && t.TokenType == QRTokenType.Normal)
            .SumAsync(t => t.AccessCount);

        // 6. Recent Activities (Mixed)
        var recentLogs = await _context.AuditLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.Timestamp)
            .Take(5)
            .Select(l => new ClinicalActivityDTO
            {
                Id = l.Id,
                Action = l.Action,
                Details = l.Details ?? string.Empty,
                Timestamp = l.Timestamp,
                Type = "Audit"
            })
            .ToListAsync();

        var recentScans = await _context.ScanHistories
            .Include(s => s.Doctor)
            .ThenInclude(d => d.User)
            .Where(s => s.PatientId == patientId)
            .OrderByDescending(s => s.ScannedAt)
            .Take(5)
            .Select(s => new ClinicalActivityDTO
            {
                Id = s.Id,
                Action = "Security Scan",
                Details = $"Profile scanned by Dr. {s.Doctor.User.FirstName} {s.Doctor.User.LastName}",
                Timestamp = s.ScannedAt,
                Type = "Security"
            })
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
