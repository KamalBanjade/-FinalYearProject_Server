using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureMedicalRecordSystem.Core.DTOs.Doctor;
using SecureMedicalRecordSystem.Core.Enums;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Infrastructure.Data;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class DoctorStatisticsService : IDoctorStatisticsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DoctorStatisticsService> _logger;

    public DoctorStatisticsService(
        ApplicationDbContext context,
        ILogger<DoctorStatisticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DoctorDashboardStatsDTO> GetDashboardStatisticsAsync(Guid doctorId)
    {
        var today = DateTime.UtcNow.Date;
        var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);
        var thisMonthStart = new DateTime(today.Year, today.Month, 1);

        // 0. Fetch Doctor Name
        var docInfo = await _context.Doctors
            .AsNoTracking()
            .Include(d => d.User)
            .Where(d => d.Id == doctorId)
            .Select(d => d.User.FirstName)
            .FirstOrDefaultAsync() ?? "Doctor";

        // 1. Fetch Lightweight Projections Sequentially
        var appointments = await _context.Appointments
            .AsNoTracking()
            .Where(a => a.DoctorId == doctorId && a.AppointmentDate >= thisMonthStart)
            .Select(a => new { a.AppointmentDate, a.Status, a.PatientId })
            .ToListAsync();

        var medicalRecords = await _context.MedicalRecords
            .AsNoTracking()
            .Where(r => r.AssignedDoctorId == doctorId)
            .Select(r => new { r.CreatedAt, r.State, r.PatientId })
            .ToListAsync();

        var recentScans = await _context.ScanHistories
            .CountAsync(s => s.DoctorId == doctorId && s.ScannedAt >= DateTime.UtcNow.AddHours(-24));

        // 2. Memory Aggregation
        var todayAppointments = appointments.Count(a => a.AppointmentDate.Date == today);
        var pendingRecords = medicalRecords.Count(r => r.State == RecordState.Pending || r.State == RecordState.Draft);
        var weekAppointments = appointments.Count(a => a.AppointmentDate >= thisWeekStart && a.AppointmentDate < thisWeekStart.AddDays(7));
        var completedThisWeek = appointments.Count(a => a.AppointmentDate >= thisWeekStart && a.AppointmentDate < thisWeekStart.AddDays(7) && a.Status == AppointmentStatus.Completed);
        
        var apptPatientIds = appointments.Where(a => a.Status == AppointmentStatus.Completed).Select(a => a.PatientId).Distinct();
        var recordPatientIds = medicalRecords.Where(r => r.CreatedAt >= thisMonthStart).Select(r => r.PatientId).Distinct();
        var monthPatients = apptPatientIds.Union(recordPatientIds).Distinct().Count();

        var completionRate = weekAppointments > 0
            ? (decimal)completedThisWeek / weekAppointments * 100
            : 0;

        return new DoctorDashboardStatsDTO
        {
            FirstName = docInfo,
            TodayAppointments = todayAppointments,
            PendingRecords = pendingRecords,
            WeekAppointments = weekAppointments,
            MonthPatients = monthPatients,
            RecentScans = recentScans,
            CompletionRate = Math.Round(completionRate, 1)
        };
    }

    public async Task<List<TodayAppointmentDTO>> GetTodayScheduleAsync(Guid doctorId)
    {
        var today = DateTime.UtcNow.Date;

        var appointments = await _context.Appointments
            .Include(a => a.Patient)
            .ThenInclude(p => p.User)
            .Where(a => a.DoctorId == doctorId &&
                       a.AppointmentDate.Date == today)
            .OrderBy(a => a.AppointmentDate)
            .Select(a => new TodayAppointmentDTO
            {
                Id = a.Id,
                PatientId = a.PatientId,
                PatientName = $"{a.Patient.User.FirstName} {a.Patient.User.LastName}",
                AppointmentTime = a.AppointmentDate,
                Status = a.Status.ToString(),
                Type = a.ReasonForVisit ?? "Regular",
                Duration = a.Duration
            })
            .ToListAsync();

        return appointments;
    }

    public async Task<WeekScheduleDTO> GetWeekScheduleAsync(Guid doctorId)
    {
        var today = DateTime.UtcNow.Date;
        var startOfPeriod = today.AddDays(-15); 
        var endOfPeriod = today.AddDays(16);

        var appointments = await _context.Appointments
            .Where(a => a.DoctorId == doctorId &&
                       a.AppointmentDate >= startOfPeriod &&
                       a.AppointmentDate < endOfPeriod)
            .GroupBy(a => a.AppointmentDate.Date)
            .Select(g => new DayScheduleDTO
            {
                Date = g.Key,
                Count = g.Count(),
                // Map nearly everything that isn't Cancelled/NoShow to Scheduled for high-level overview
                Scheduled = g.Count(a => a.Status == AppointmentStatus.Confirmed || 
                                       a.Status == AppointmentStatus.Scheduled || 
                                       a.Status == AppointmentStatus.InProgress ||
                                       a.Status == AppointmentStatus.Rescheduled),
                Completed = g.Count(a => a.Status == AppointmentStatus.Completed),
                Cancelled = g.Count(a => a.Status == AppointmentStatus.Cancelled || 
                                       a.Status == AppointmentStatus.NoShow)
            })
            .ToListAsync();

        // Fill all 31 days in the range (-15 to +15)
        var allDays = Enumerable.Range(0, 31)
            .Select(i => startOfPeriod.AddDays(i))
            .Select(date => appointments.FirstOrDefault(a => a.Date == date) ?? new DayScheduleDTO
            {
                Date = date,
                Count = 0,
                Scheduled = 0,
                Completed = 0,
                Cancelled = 0
            })
            .OrderBy(d => d.Date)
            .ToList();

        return new WeekScheduleDTO
        {
            WeekStart = startOfPeriod,
            Days = allDays
        };
    }

    public async Task<List<PatientVolumeTrendDTO>> GetPatientVolumeTrendAsync(Guid doctorId)
    {
        var thirtyDaysAgo = DateTime.UtcNow.Date.AddDays(-30);

        // Fetch completed appointments in last 30 days
        var appointmentActivity = await _context.Appointments
            .Where(a => a.DoctorId == doctorId && a.AppointmentDate >= thirtyDaysAgo && a.Status == AppointmentStatus.Completed)
            .Select(a => new { Date = a.AppointmentDate.Date, a.PatientId })
            .ToListAsync();

        // Fetch medical record assignments in last 30 days
        var recordActivity = await _context.MedicalRecords
            .Where(r => r.AssignedDoctorId == doctorId && r.CreatedAt >= thirtyDaysAgo)
            .Select(r => new { Date = r.CreatedAt.Date, r.PatientId })
            .ToListAsync();

        // Merge activities
        var allActivity = appointmentActivity.Union(recordActivity).ToList();

        var trend = allActivity
            .GroupBy(x => x.Date)
            .Select(g => new PatientVolumeTrendDTO
            {
                Date = g.Key,
                PatientCount = g.Select(x => x.PatientId).Distinct().Count()
            })
            .OrderBy(t => t.Date)
            .ToList();

        return trend;
    }

    public async Task<RecordStatusBreakdownDTO> GetRecordStatusBreakdownAsync(Guid doctorId)
    {
        var records = await _context.MedicalRecords
            .Where(r => r.AssignedDoctorId == doctorId)
            .GroupBy(r => r.State)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return new RecordStatusBreakdownDTO
        {
            Draft = records.FirstOrDefault(r => r.Status == RecordState.Draft)?.Count ?? 0,
            Pending = records.FirstOrDefault(r => r.Status == RecordState.Pending)?.Count ?? 0,
            Certified = records.FirstOrDefault(r => r.Status == RecordState.Certified)?.Count ?? 0,
            Total = records.Sum(r => r.Count)
        };
    }

    public async Task<List<RecentScanDTO>> GetRecentScansAsync(Guid doctorId, int limit)
    {
        var scans = await _context.ScanHistories
            .Include(s => s.Patient)
            .ThenInclude(p => p.User)
            .Where(s => s.DoctorId == doctorId)
            .OrderByDescending(s => s.ScannedAt)
            .Take(limit)
            .Select(s => new RecentScanDTO
            {
                Id = s.Id,
                PatientId = s.PatientId,
                PatientName = $"{s.Patient.User.FirstName} {s.Patient.User.LastName}",
                ScannedAt = s.ScannedAt,
                IsEmergency = s.AccessGranted && s.DesktopSessionId != null,
                TOTPVerified = s.TOTPVerified
            })
            .ToListAsync();

        return scans;
    }

    public async Task<List<TemplateUsageDTO>> GetTemplateUsageAsync(Guid doctorId)
    {
        // Optimized to use a single group-by query to avoid N+1
        var usage = await _context.PatientHealthRecords
            .Where(hr => hr.DoctorId == doctorId && hr.TemplateId != null)
            .GroupBy(hr => new { hr.TemplateId, TemplateName = hr.Template != null ? hr.Template.TemplateName : "Unknown" })
            .Select(g => new TemplateUsageDTO
            {
                TemplateId = g.Key.TemplateId!.Value,
                TemplateName = g.Key.TemplateName,
                UsageCount = g.Count()
            })
            .OrderByDescending(t => t.UsageCount)
            .Take(10)
            .ToListAsync();

        return usage;
    }

    public async Task<List<RecordGrowthTrendDTO>> GetRecordGrowthTrendAsync(Guid doctorId)
    {
        // 1. Determine Earliest Record to define "Practice Age"
        var earliestRecord = await _context.MedicalRecords
            .Where(r => r.AssignedDoctorId == doctorId)
            .OrderBy(r => r.CreatedAt)
            .Select(r => (DateTime?)r.CreatedAt)
            .FirstOrDefaultAsync();

        if (earliestRecord == null)
        {
            var today = DateTime.UtcNow.Date;
            return new List<RecordGrowthTrendDTO> 
            { 
                new RecordGrowthTrendDTO { Label = today.AddDays(-14).ToString("MMM dd"), Total = 0 },
                new RecordGrowthTrendDTO { Label = today.ToString("MMM dd"), Total = 0 }
            };
        }

        var ageInDays = (DateTime.UtcNow.Date - earliestRecord.Value.Date).TotalDays;
        var trend = new List<RecordGrowthTrendDTO>();

        // 2. Select Resolution & Aggregation
        if (ageInDays <= 45) // Daily Mode (Show entire data from start)
        {
            var startDate = earliestRecord.Value.Date;
            var dailyData = await _context.MedicalRecords
                .Where(r => r.AssignedDoctorId == doctorId && r.CreatedAt >= startDate)
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
            int daysCount = (int)(DateTime.UtcNow.Date - startDate).TotalDays;
            for (int i = 0; i <= daysCount; i++)
            {
                var d = startDate.AddDays(i);
                var m = dailyData.FirstOrDefault(x => x.Date == d);
                if (m != null) { cT += m.Total; cC += m.Cert; cP += m.Pend; cD += m.Draft; cE += m.Emerg; cA += m.Arch; }
                trend.Add(new RecordGrowthTrendDTO { Label = d.ToString("MMM dd"), Resolution = "Day", Total = cT, Certified = cC, Pending = cP, Draft = cD, Emergency = cE, Archived = cA });
            }
        }
        else if (ageInDays <= 180) // Weekly Mode
        {
            var startDate = earliestRecord.Value.Date.AddDays(-(int)earliestRecord.Value.DayOfWeek); // Start of week
            var records = await _context.MedicalRecords
                .Where(r => r.AssignedDoctorId == doctorId)
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

                trend.Add(new RecordGrowthTrendDTO { Label = $"W{GetIso8601WeekOfYear(current)}", Resolution = "Week", Total = cT, Certified = cC, Pending = cP, Draft = cD, Emergency = cE, Archived = cA });
                current = next;
            }
        }
        else if (ageInDays <= 730) // Monthly Mode
        {
            var startDate = new DateTime(earliestRecord.Value.Year, earliestRecord.Value.Month, 1);
            var records = await _context.MedicalRecords
                .Where(r => r.AssignedDoctorId == doctorId)
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

                trend.Add(new RecordGrowthTrendDTO { Label = current.ToString("MMM yy"), Resolution = "Month", Total = cT, Certified = cC, Pending = cP, Draft = cD, Emergency = cE, Archived = cA });
                current = next;
            }
        }
        else // Yearly Mode
        {
            var startDate = new DateTime(earliestRecord.Value.Year, 1, 1);
            var records = await _context.MedicalRecords
                .Where(r => r.AssignedDoctorId == doctorId)
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

                trend.Add(new RecordGrowthTrendDTO { Label = current.ToString("yyyy"), Resolution = "Year", Total = cT, Certified = cC, Pending = cP, Draft = cD, Emergency = cE, Archived = cA });
                current = next;
            }
        }

        return trend;
    }

    private static int GetIso8601WeekOfYear(DateTime time)
    {
        // Use ISO 8601 definition of a week
        System.DayOfWeek day = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
        if (day >= System.DayOfWeek.Monday && day <= System.DayOfWeek.Wednesday) { time = time.AddDays(3); }
        return System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, System.Globalization.CalendarWeekRule.FirstFourDayWeek, System.DayOfWeek.Monday);
    }
    }

