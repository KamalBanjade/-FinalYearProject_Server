using Microsoft.EntityFrameworkCore;
using SecureMedicalRecordSystem.Core.DTOs.Appointments;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Core.Enums;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Infrastructure.Data;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class DoctorAvailabilityService : IDoctorAvailabilityService
{
    private readonly ApplicationDbContext _context;

    public DoctorAvailabilityService(ApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<(bool Success, string Message)> SetWorkingHoursAsync(
        Guid doctorId, 
        DayOfWeek dayOfWeek, 
        TimeSpan startTime, 
        TimeSpan endTime,
        TimeSpan? breakStartTime = null,
        TimeSpan? breakEndTime = null)
    {
        var existing = await _context.DoctorAvailabilities
            .Where(a => a.DoctorId == doctorId && 
                        a.DayOfWeek == (int)dayOfWeek && 
                        a.RecurrenceType == RecurrenceType.Weekly)
            .ToListAsync();

        if (existing.Any())
        {
            _context.DoctorAvailabilities.RemoveRange(existing);
        }

        if (breakStartTime.HasValue && breakEndTime.HasValue)
        {
            // Validate break
            if (breakStartTime < startTime || breakEndTime > endTime || breakStartTime >= breakEndTime)
            {
                return (false, "Break time must be within working hours and valid.");
            }

            // Shift 1
            if (breakStartTime > startTime)
            {
                _context.DoctorAvailabilities.Add(new DoctorAvailability
                {
                    DoctorId = doctorId,
                    DayOfWeek = (int)dayOfWeek,
                    StartTime = startTime,
                    EndTime = breakStartTime.Value,
                    IsAvailable = true,
                    RecurrenceType = RecurrenceType.Weekly,
                    IsActive = true
                });
            }

            // Shift 2
            if (breakEndTime < endTime)
            {
                _context.DoctorAvailabilities.Add(new DoctorAvailability
                {
                    DoctorId = doctorId,
                    DayOfWeek = (int)dayOfWeek,
                    StartTime = breakEndTime.Value,
                    EndTime = endTime,
                    IsAvailable = true,
                    RecurrenceType = RecurrenceType.Weekly,
                    IsActive = true
                });
            }
        }
        else
        {
            var availability = new DoctorAvailability
            {
                DoctorId = doctorId,
                DayOfWeek = (int)dayOfWeek,
                StartTime = startTime,
                EndTime = endTime,
                IsAvailable = true,
                RecurrenceType = RecurrenceType.Weekly,
                IsActive = true
            };
            _context.DoctorAvailabilities.Add(availability);
        }

        await _context.SaveChangesAsync();
        return (true, "Working hours updated successfully.");
    }

    public async Task<(bool Success, string Message)> BlockTimeAsync(
        Guid doctorId, 
        DateTime startDateTime, 
        DateTime endDateTime, 
        string reason)
    {
        var availability = new DoctorAvailability
        {
            DoctorId = doctorId,
            SpecificDate = startDateTime.Date,
            StartTime = startDateTime.TimeOfDay,
            EndTime = endDateTime.TimeOfDay,
            IsAvailable = false,
            RecurrenceType = RecurrenceType.OneTime,
            Reason = reason,
            IsActive = true
        };

        _context.DoctorAvailabilities.Add(availability);
        await _context.SaveChangesAsync();

        return (true, "Time blocked successfully.");
    }

    public async Task<(bool Success, string Message)> UnblockTimeAsync(
        Guid availabilityId,
        Guid requestingDoctorUserId)
    {
        var availability = await _context.DoctorAvailabilities
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == availabilityId);

        if (availability == null) return (false, "Availability record not found.");
        
        if (availability.Doctor.UserId != requestingDoctorUserId)
            return (false, "You are not authorized to manage this schedule.");

        _context.DoctorAvailabilities.Remove(availability);
        await _context.SaveChangesAsync();

        return (true, "Record removed successfully.");
    }

    public async Task<List<DoctorAvailabilityDTO>> GetDoctorScheduleAsync(
        Guid doctorId, 
        DateTime startDate, 
        DateTime endDate)
    {
        var availabilities = await _context.DoctorAvailabilities
            .Where(a => a.DoctorId == doctorId && a.IsActive)
            .Where(a => (a.RecurrenceType == RecurrenceType.Weekly) || 
                        (a.SpecificDate >= startDate.Date && a.SpecificDate <= endDate.Date))
            .ToListAsync();

        return availabilities.Select(a => new DoctorAvailabilityDTO
        {
            Id = a.Id,
            DoctorId = a.DoctorId,
            DayOfWeek = a.DayOfWeek,
            SpecificDate = a.SpecificDate,
            StartTime = a.StartTime,
            EndTime = a.EndTime,
            IsAvailable = a.IsAvailable,
            RecurrenceType = a.RecurrenceType,
            Reason = a.Reason
        }).ToList();
    }

    public async Task<bool> IsDoctorAvailableAsync(
        Guid doctorId, 
        DateTime dateTime, 
        int durationMinutes)
    {
        // Normalize requested time to UTC
        var requestedUtc = dateTime.Kind == DateTimeKind.Unspecified 
            ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc) 
            : dateTime.ToUniversalTime();

        // IMPORTANT: Convert to local time before calling GetAvailableSlots
        // because slot generation is based on local wall-clock working hours.
        // If we pass a UTC DateTime, the .Date would give us the wrong calendar day.
        var requestedLocal = requestedUtc.ToLocalTime();

        var slots = await GetAvailableSlotsWithRulesAsync(doctorId, requestedLocal, durationMinutes);
        
        // Compare in UTC to avoid timezone issues
        return slots.Any(s => s.IsAvailable && Math.Abs((s.StartTime - requestedUtc).TotalMinutes) < 1);
    }

    public async Task<List<TimeSlotDTO>> GetAvailableSlotsWithRulesAsync(
        Guid doctorId,
        DateTime date,
        int duration = 30)
    {
        var localDate = date.Kind == DateTimeKind.Utc
            ? date.ToLocalTime().Date
            : date.ToLocalTime().Date;
        var dayOfWeek = (int)localDate.DayOfWeek;

        // === LAYER 1: Determine working shifts for this specific date ===
        // Priority: OneTime Overrides (IsAvailable=true for this date) > Weekly Shifts
        var dateOverrideShifts = await _context.DoctorAvailabilities
            .Where(a => a.DoctorId == doctorId &&
                        a.IsActive &&
                        a.IsAvailable &&
                        a.RecurrenceType == RecurrenceType.OneTime &&
                        a.SpecificDate == localDate)
            .ToListAsync();

        List<DoctorAvailability> activeShifts;
        if (dateOverrideShifts.Any())
        {
            // A date-specific override exists → use it instead of weekly schedule
            activeShifts = dateOverrideShifts;
        }
        else
        {
            // No override → fall back to weekly recurring shifts for this day-of-week
            activeShifts = await _context.DoctorAvailabilities
                .Where(a => a.DoctorId == doctorId &&
                            a.IsActive &&
                            a.IsAvailable &&
                            a.RecurrenceType == RecurrenceType.Weekly &&
                            a.DayOfWeek == dayOfWeek)
                .ToListAsync();
        }

        if (!activeShifts.Any()) return new List<TimeSlotDTO>();

        // === LAYER 2: Generate candidate slots within each shift window ===
        // Gap between shifts (e.g. 12:00–13:00 lunch) automatically produces no slots.
        var allSlotsUtc = new List<DateTime>();
        foreach (var shift in activeShifts)
        {
            var shiftStart = DateTime.SpecifyKind(localDate.Add(shift.StartTime), DateTimeKind.Local).ToUniversalTime();
            var shiftEnd   = DateTime.SpecifyKind(localDate.Add(shift.EndTime),   DateTimeKind.Local).ToUniversalTime();
            var slotEnd    = shiftEnd.AddMinutes(-duration); // last valid start

            var current = shiftStart;
            while (current <= slotEnd)
            {
                allSlotsUtc.Add(current);
                current = current.AddMinutes(15);
            }
        }

        // Sort chronologically across all shifts
        allSlotsUtc.Sort();

        // === LAYER 3: Fetch blocks & appointments ===

        // One-Time absence blocks for this date
        var oneTimeBlocks = await _context.DoctorAvailabilities
            .Where(a => a.DoctorId == doctorId &&
                        a.IsActive &&
                        !a.IsAvailable &&
                        a.RecurrenceType == RecurrenceType.OneTime &&
                        a.SpecificDate == localDate)
            .ToListAsync();

        // Daily recurring blocks (e.g. admin meetings every day)
        var dailyBlocks = await _context.DoctorAvailabilities
            .Where(a => a.DoctorId == doctorId &&
                        a.IsActive &&
                        !a.IsAvailable &&
                        a.RecurrenceType == RecurrenceType.Daily)
            .ToListAsync();

        // Existing booked appointments (±1 day buffer for UTC edge cases)
        var windowStart = localDate.AddDays(-1);
        var windowEnd   = localDate.AddDays(2);
        var bookedAppointments = await _context.Appointments
            .Where(a => a.DoctorId == doctorId &&
                        a.AppointmentDate >= windowStart &&
                        a.AppointmentDate <  windowEnd &&
                        a.IsActive &&
                        !a.IsCancelled)
            .ToListAsync();

        // === LAYER 4: Map slots to DTOs, marking unavailable ones ===
        var nowUtc = DateTime.UtcNow;

        return allSlotsUtc.Select(slot =>
        {
            var slotEnds = slot.AddMinutes(duration);
            bool isAvailable = true;

            // Past / too-soon slots (30-min buffer)
            if (slot <= nowUtc.AddMinutes(30))
                isAvailable = false;

            // Absence overlap: standard interval-overlap formula
            // A slot [slot, slotEnd) overlaps block [bStart, bEnd) when:
            //   slot < bEnd  &&  slotEnd > bStart
            if (isAvailable)
            {
                var slotLocal    = slot.ToLocalTime().TimeOfDay;
                var slotEndLocal = slotEnds.ToLocalTime().TimeOfDay;

                if (oneTimeBlocks.Any(b => slotLocal < b.EndTime && slotEndLocal > b.StartTime) ||
                    dailyBlocks.Any( b => slotLocal < b.EndTime && slotEndLocal > b.StartTime))
                    isAvailable = false;
            }

            // Booked appointment conflict
            if (isAvailable)
            {
                bool hasConflict = bookedAppointments.Any(a =>
                {
                    var apptStart = DateTime.SpecifyKind(a.AppointmentDate, DateTimeKind.Utc);
                    var apptEnd   = apptStart.AddMinutes(a.Duration);
                    return slot < apptEnd && slotEnds > apptStart;
                });
                if (hasConflict) isAvailable = false;
            }

            return new TimeSlotDTO { StartTime = slot, EndTime = slotEnds, IsAvailable = isAvailable };
        }).ToList();
    }

    public async Task<List<DailyAvailabilityDTO>> GetMonthlyAvailabilityAsync(Guid doctorId, int year, int month)
    {
        var result = new List<DailyAvailabilityDTO>();
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var today = DateTime.UtcNow.ToLocalTime().Date;

        // Pre-fetch all weekly shifts for this doctor
        var weeklyShifts = await _context.DoctorAvailabilities
            .Where(a => a.DoctorId == doctorId && a.IsActive && a.IsAvailable && a.RecurrenceType == RecurrenceType.Weekly)
            .ToListAsync();

        // Pre-fetch one-time overrides and full-day blocks that fall within the month
        var monthStart = new DateTime(year, month, 1);
        var monthEnd   = monthStart.AddMonths(1);

        var oneTimeEntries = await _context.DoctorAvailabilities
            .Where(a => a.DoctorId == doctorId && a.IsActive &&
                        a.RecurrenceType == RecurrenceType.OneTime &&
                        a.SpecificDate >= monthStart && a.SpecificDate < monthEnd)
            .ToListAsync();

        for (int day = 1; day <= daysInMonth; day++)
        {
            var localDate  = new DateTime(year, month, day);
            var dayOfWeek  = (int)localDate.DayOfWeek;
            bool available = false;

            // Past days are never shown as available
            if (localDate >= today)
            {
                // Check if there's a one-time available override for this date
                bool hasOverride   = oneTimeEntries.Any(e => e.SpecificDate == localDate && e.IsAvailable);
                // Check if there's a full-day one-time block (e.g. vacation day)
                bool hasFullDayBlock = oneTimeEntries.Any(e =>
                    e.SpecificDate == localDate &&
                    !e.IsAvailable &&
                    e.StartTime == TimeSpan.Zero && e.EndTime >= TimeSpan.FromHours(23));

                if (hasFullDayBlock)
                {
                    available = false; // Explicit full-day absence wins
                }
                else if (hasOverride)
                {
                    available = true;  // Date-specific working shift
                }
                else
                {
                    // Fall back to weekly schedule
                    available = weeklyShifts.Any(s => s.DayOfWeek == dayOfWeek);
                }
            }

            result.Add(new DailyAvailabilityDTO { Date = localDate, IsAvailable = available });
        }

        return result;
    }
}

