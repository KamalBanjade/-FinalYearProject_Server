using SecureMedicalRecordSystem.Core.Entities;

namespace SecureMedicalRecordSystem.Core.Interfaces;

public interface IEmailService
{
    Task<bool> SendDoctorInvitationEmailAsync(string toEmail, string doctorName, string temporaryPassword, string resetLink);
    Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink);
    Task<bool> SendEmailConfirmationAsync(string toEmail, string confirmationLink);
    Task<bool> SendSecurityAlertEmailAsync(string toEmail, string subject, string message);
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody);
    
    // Appointment Notifications
    Task<bool> SendAppointmentConfirmationEmailAsync(string toEmail, Appointment appointment);
    Task<bool> SendAppointmentConfirmedEmailAsync(string toEmail, Appointment appointment);
    Task<bool> SendAppointmentCancelledEmailAsync(string toEmail, Appointment appointment, string reason);
    Task<bool> SendAppointmentRescheduledEmailAsync(string toEmail, Appointment appointment);
    Task<bool> SendAppointmentReminderEmailAsync(string toEmail, Appointment appointment);
    Task<bool> SendDoctorNewAppointmentNotificationAsync(string toEmail, Appointment appointment);
}
