namespace SecureMedicalRecordSystem.Core.Interfaces;

public interface IEmailService
{
    Task<bool> SendDoctorInvitationEmailAsync(string toEmail, string doctorName, string temporaryPassword, string resetLink);
    Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink);
    Task<bool> SendEmailConfirmationAsync(string toEmail, string confirmationLink);
    Task<bool> SendSecurityAlertEmailAsync(string toEmail, string subject, string message);
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody);
}
