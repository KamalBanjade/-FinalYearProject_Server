using SecureMedicalRecordSystem.Core.DTOs.Appointments;
using SecureMedicalRecordSystem.Core.DTOs.MedicalRecords;

namespace SecureMedicalRecordSystem.Core.Interfaces;

public interface IAppointmentService
{
    Task<(bool Success, string Message, AppointmentDTO? Data)> CreateAppointmentAsync(
        CreateAppointmentDTO request, 
        Guid requestingUserId);

    Task<(bool Success, string Message, List<AppointmentDTO>? Data)> GetPatientAppointmentsAsync(
        Guid patientId, 
        Guid requestingUserId, 
        bool includeHistory = false);

    Task<(bool Success, string Message, AppointmentDTO? Data)> GetAppointmentByIdAsync(
        Guid appointmentId, 
        Guid requestingUserId);

    Task<(bool Success, string Message, List<AppointmentDTO>? Data)> GetDoctorAppointmentsAsync(
        Guid doctorId, 
        Guid requestingUserId, 
        DateTime? date = null,
        bool includeHistory = false);

    Task<(bool Success, string Message, DoctorAppointmentStatsDTO? Data)> GetDoctorStatsAsync(
        Guid doctorId,
        Guid requestingUserId);

    Task<(bool Success, string Message)> CancelAppointmentAsync(
        Guid appointmentId, 
        string cancellationReason, 
        Guid requestingUserId);

    Task<(bool Success, string Message, AppointmentDTO? Data)> RescheduleAppointmentAsync(
        Guid appointmentId, 
        DateTime newDateTime, 
        Guid requestingUserId);

    Task<(bool Success, string Message)> CompleteAppointmentAsync(
        Guid appointmentId, 
        string consultationNotes, 
        Guid requestingUserId);

    Task<(bool Success, string Message)> ConfirmAppointmentAsync(
        Guid appointmentId, 
        Guid requestingUserId);

    Task<(bool Success, string Message)> LinkRecordToAppointmentAsync(
        Guid appointmentId, 
        Guid medicalRecordId, 
        string notes, 
        Guid requestingUserId);

    Task<List<TimeSlotDTO>> GetDoctorAvailableSlotsAsync(
        Guid doctorId, 
        DateTime date);

    Task<bool> CheckAppointmentConflictAsync(
        Guid doctorId, 
        DateTime appointmentDateTime, 
        int duration = 30,
        Guid? excludeAppointmentId = null);

    Task<int> CheckAndTransitionAppointmentStatusesAsync();

    Task<(bool Success, string Message, SmartDoctorSuggestionDTO? Data)> GetSmartDoctorSuggestionsAsync(
        Guid requestingUserId);

    Task<(bool Success, string Message, List<DoctorSuggestionItem>? Data)> SuggestDoctorsByReasonAsync(
        string reason);

    Task<FollowUpAppointmentResult> ScheduleFollowUpAppointmentAsync(
        Guid? originalAppointmentId,
        DateTime preferredDate,
        Guid doctorId,
        Guid patientId,
        int duration);
}
