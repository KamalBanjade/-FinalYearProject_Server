using SecureMedicalRecordSystem.Core.DTOs.Appointments;

namespace SecureMedicalRecordSystem.Core.Interfaces;

public interface IDoctorAvailabilityService
{
    Task<(bool Success, string Message)> SetWorkingHoursAsync(
        Guid doctorId, 
        DayOfWeek dayOfWeek, 
        TimeSpan startTime, 
        TimeSpan endTime,
        TimeSpan? breakStartTime = null,
        TimeSpan? breakEndTime = null);

    Task<(bool Success, string Message)> BlockTimeAsync(
        Guid doctorId, 
        DateTime startDateTime, 
        DateTime endDateTime, 
        string reason);

    Task<(bool Success, string Message)> UnblockTimeAsync(
        Guid availabilityId,
        Guid requestingDoctorUserId);

    Task<List<DoctorAvailabilityDTO>> GetDoctorScheduleAsync(
        Guid doctorId, 
        DateTime startDate, 
        DateTime endDate);

    Task<bool> IsDoctorAvailableAsync(
        Guid doctorId, 
        DateTime dateTime, 
        int durationMinutes);

    Task<List<TimeSlotDTO>> GetAvailableSlotsWithRulesAsync(
        Guid doctorId,
        DateTime date,
        int duration = 30);

    Task<List<DailyAvailabilityDTO>> GetMonthlyAvailabilityAsync(
        Guid doctorId,
        int year,
        int month);
}
