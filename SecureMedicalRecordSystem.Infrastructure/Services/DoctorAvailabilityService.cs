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
        TimeSpan endTime)
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
        // Always work with the LOCAL calendar date so dayOfWeek matches the doctor's schedule
        var localDate = date.Kind == DateTimeKind.Utc 
            ? date.ToLocalTime().Date 
            : date.ToLocalTime().Date;
        var inputDate = localDate;
        var dayOfWeek = (int)inputDate.DayOfWeek;
        
        // 1. Get Weekly working hours
        var workingHours = await _context.DoctorAvailabilities
            .Where(a => a.DoctorId == doctorId && 
                        a.IsActive && 
                        a.IsAvailable && 
                        a.RecurrenceType == RecurrenceType.Weekly && 
                        a.DayOfWeek == dayOfWeek)
            .FirstOrDefaultAsync();

        if (workingHours == null) return new List<TimeSlotDTO>();

        // 2. Generate base candidate slots at 15-min granularity
        var allSlots = new List<DateTime>();
        var localStart = DateTime.SpecifyKind(inputDate.Date.Add(workingHours.StartTime), DateTimeKind.Local);
        var localEnd = DateTime.SpecifyKind(inputDate.Date.Add(workingHours.EndTime), DateTimeKind.Local);
        
        var currentSlotUtc = localStart.ToUniversalTime();
        var endTimeUtc = localEnd.ToUniversalTime().AddMinutes(-duration);

        while (currentSlotUtc <= endTimeUtc)
        {
            allSlots.Add(currentSlotUtc);
            currentSlotUtc = currentSlotUtc.AddMinutes(15);
        }

        // 3. Get One-Time Blocks
        var oneTimeBlocks = await _context.DoctorAvailabilities
            .Where(a => a.DoctorId == doctorId && 
                        a.IsActive && 
                        !a.IsAvailable && 
                        a.RecurrenceType == RecurrenceType.OneTime && 
                        a.SpecificDate == inputDate)
            .ToListAsync();

        // 4. Get Recurring Blocks
        var recurringBlocks = await _context.DoctorAvailabilities
            .Where(a => a.DoctorId == doctorId && 
                        a.IsActive && 
                        !a.IsAvailable && 
                        a.RecurrenceType == RecurrenceType.Daily)
            .ToListAsync();

        // 5. Get Existing Appointments
        var startDate = inputDate.AddDays(-1);
        var endDate = inputDate.AddDays(1);
        
        var existingAppointments = await _context.Appointments
            .Where(a => a.DoctorId == doctorId && 
                        a.AppointmentDate >= startDate && 
                        a.AppointmentDate <= endDate && 
                        a.IsActive && 
                        !a.IsCancelled)
            .ToListAsync();

        // 6. Map to DTOs with availability flags
        var nowUtc = DateTime.UtcNow;
        return allSlots.Select(slot =>
        {
            var slotEnd = slot.AddMinutes(duration);
            
            bool isAvailable = true;

            // Check past slots
            if (slot <= nowUtc.AddMinutes(30))
            {
                isAvailable = false;
            }

            // Check One-Time Blocks
            if (isAvailable && oneTimeBlocks.Any(b => slot.TimeOfDay >= b.StartTime && slot.TimeOfDay < b.EndTime))
            {
                isAvailable = false;
            }

            // Check Recurring Blocks
            if (isAvailable && recurringBlocks.Any(b => slot.TimeOfDay >= b.StartTime && slot.TimeOfDay < b.EndTime))
            {
                isAvailable = false;
            }

            // Check Existing Appointments
            if (isAvailable)
            {
                var hasConflict = existingAppointments.Any(a =>
                {
                    var apptStart = DateTime.SpecifyKind(a.AppointmentDate, DateTimeKind.Utc);
                    var apptEnd = apptStart.AddMinutes(a.Duration);
                    return slot < apptEnd && slotEnd > apptStart;
                });
                if (hasConflict) isAvailable = false;
            }

            return new TimeSlotDTO 
            { 
                StartTime = slot, 
                EndTime = slotEnd, 
                IsAvailable = isAvailable 
            };
        }).ToList();
    }
}
