using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Infrastructure.Configuration;
using SecureMedicalRecordSystem.Infrastructure.Templates;
using System.Net;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;

    public EmailService(
        IOptions<SmtpSettings> settings, 
        ILogger<EmailService> logger,
        IConfiguration configuration)
    {
        _settings = settings.Value;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<bool> SendDoctorInvitationEmailAsync(string toEmail, string doctorName, string temporaryPassword, string resetLink)
    {
        try
        {
            var template = _configuration.GetSection("EmailTemplates");
            var subject = template["DoctorInvitationSubject"] ?? "Welcome to Medical Record System";
            var body = EmailTemplates.GetDoctorInvitationTemplate(doctorName, toEmail, temporaryPassword, resetLink);

            return await SendEmailAsync(toEmail, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preparing doctor invitation email for {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink)
    {
        try
        {
            var subject = "Password Reset Request - Medical Record System";
            var body = EmailTemplates.GetPasswordResetTemplate(resetLink);

            return await SendEmailAsync(toEmail, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preparing password reset email for {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendEmailConfirmationAsync(string toEmail, string confirmationLink)
    {
        try
        {
            var subject = "Verify Your Email - Medical Record System";
            var body = EmailTemplates.GetEmailConfirmationTemplate(confirmationLink);

            return await SendEmailAsync(toEmail, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preparing email confirmation for {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendSecurityAlertEmailAsync(string toEmail, string subject, string message)
    {
        try
        {
            var htmlBody = message.Replace("\n", "<br>");
            return await SendEmailAsync(toEmail, subject, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preparing security alert email for {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(new MailboxAddress("", toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(_settings.Host, _settings.Port, _settings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {Email}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendAppointmentConfirmationEmailAsync(string toEmail, Appointment appointment)
    {
        var subject = "Appointment Confirmed ✓ - Medical Record System";
        var patientName = appointment.Patient?.User != null ? $"{appointment.Patient.User.FirstName} {appointment.Patient.User.LastName}" : "Valued Patient";
        var doctorName = appointment.Doctor?.User != null ? $"{appointment.Doctor.User.FirstName} {appointment.Doctor.User.LastName}" : "the Doctor";
        
        var body = EmailTemplates.GetAppointmentScheduledTemplate(patientName, doctorName, appointment.AppointmentDate, appointment.ReasonForVisit);
        return await SendEmailWithCalendarAsync(toEmail, subject, body, appointment);
    }

    public async Task<bool> SendAppointmentConfirmedEmailAsync(string toEmail, Appointment appointment)
    {
        var subject = "Appointment Confirmed - Medical Record System";
        var patientName = appointment.Patient?.User != null ? $"{appointment.Patient.User.FirstName} {appointment.Patient.User.LastName}" : "Valued Patient";
        var doctorName = appointment.Doctor?.User != null ? $"{appointment.Doctor.User.FirstName} {appointment.Doctor.User.LastName}" : "the Doctor";
        var department = appointment.Doctor?.Department?.Name ?? "General Practice";

        var body = EmailTemplates.GetAppointmentConfirmedTemplate(patientName, doctorName, appointment.AppointmentDate, department);
        return await SendEmailWithCalendarAsync(toEmail, subject, body, appointment);
    }

    public async Task<bool> SendAppointmentCancelledEmailAsync(string toEmail, Appointment appointment, string reason)
    {
        var subject = "Appointment Cancelled - Medical Record System";
        var userName = "Valued User";
        var role = "Patient";

        if (appointment.Patient?.User?.Email == toEmail)
        {
            userName = appointment.Patient.User.FirstName;
            role = "Patient";
        }
        else if (appointment.Doctor?.User?.Email == toEmail)
        {
            userName = "Dr. " + appointment.Doctor.User.LastName;
            role = "Doctor";
        }

        var body = EmailTemplates.GetAppointmentCancelledTemplate(userName, appointment.AppointmentDate, reason, role);
        return await SendEmailAsync(toEmail, subject, body);
    }

    public async Task<bool> SendAppointmentRescheduledEmailAsync(string toEmail, Appointment appointment)
    {
        var subject = "Appointment Rescheduled - Medical Record System";
        var patientName = appointment.Patient?.User != null ? $"{appointment.Patient.User.FirstName} {appointment.Patient.User.LastName}" : "Valued Patient";
        var doctorName = appointment.Doctor?.User != null ? $"{appointment.Doctor.User.FirstName} {appointment.Doctor.User.LastName}" : "the Doctor";

        // Note: For simplicity, assuming the "oldDate" isn't easily accessible here unless passed.
        // We'll just show the new date in the template for now.
        var body = EmailTemplates.GetAppointmentRescheduledTemplate(patientName, doctorName, appointment.AppointmentDate, appointment.AppointmentDate);
        return await SendEmailWithCalendarAsync(toEmail, subject, body, appointment);
    }

    public async Task<bool> SendAppointmentReminderEmailAsync(string toEmail, Appointment appointment)
    {
        var subject = "Appointment Reminder - Medical Record System";
        var patientName = appointment.Patient?.User != null ? $"{appointment.Patient.User.FirstName} {appointment.Patient.User.LastName}" : "Valued Patient";
        var doctorName = appointment.Doctor?.User != null ? $"{appointment.Doctor.User.FirstName} {appointment.Doctor.User.LastName}" : "the Doctor";

        var body = EmailTemplates.GetAppointmentReminderTemplate(patientName, doctorName, appointment.AppointmentDate);
        return await SendEmailWithCalendarAsync(toEmail, subject, body, appointment);
    }

    public async Task<bool> SendDoctorNewAppointmentNotificationAsync(string toEmail, Appointment appointment)
    {
        var doctorName = appointment.Doctor?.User?.LastName ?? "Doctor";
        var patientName = appointment.Patient?.User != null ? $"{appointment.Patient.User.FirstName} {appointment.Patient.User.LastName}" : "a Patient";
        var subject = $"New Appointment: {patientName} - {appointment.AppointmentDate:f}";
        
        var body = EmailTemplates.GetDoctorNewAppointmentTemplate(doctorName, patientName, appointment.AppointmentDate, appointment.ReasonForVisit ?? "Not specified");
        return await SendEmailWithCalendarAsync(toEmail, subject, body, appointment);
    }

    private async Task<bool> SendEmailWithCalendarAsync(string toEmail, string subject, string htmlBody, Appointment appointment)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };

            // Create .ics file
            var icsContent = GenerateIcsContent(appointment);
            var calendarBytes = System.Text.Encoding.UTF8.GetBytes(icsContent);
            bodyBuilder.Attachments.Add("appointment.ics", calendarBytes, new ContentType("text", "calendar"));

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port, _settings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email with calendar to {Email}", toEmail);
            return false;
        }
    }

    private string GenerateIcsContent(Appointment appointment)
    {
        var start = appointment.AppointmentDate.ToString("yyyyMMddTHHmmssZ");
        var end = appointment.AppointmentDate.AddMinutes(appointment.Duration).ToString("yyyyMMddTHHmmssZ");
        var now = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
        var doctorName = appointment.Doctor?.User != null ? $"Dr. {appointment.Doctor.User.FirstName} {appointment.Doctor.User.LastName}" : "Doctor";

        return $@"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//SecureMedicalRecordSystem//Appointment//EN
BEGIN:VEVENT
UID:{appointment.Id}
DTSTAMP:{now}
DTSTART:{start}
DTEND:{end}
SUMMARY:Medical Appointment with {doctorName}
DESCRIPTION:Reason for visit: {appointment.ReasonForVisit}
LOCATION:Medical Center
END:VEVENT
END:VCALENDAR";
    }

    public async Task SendFollowUpConfirmationAsync(
        string patientEmail,
        string patientName,
        string doctorName,
        DateTime followUpDate,
        int durationMinutes,
        Guid appointmentId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(patientEmail)) return;

            var subject = $"Follow-Up Appointment Confirmed — {followUpDate.ToLocalTime():MMMM d, yyyy}";
            var body = EmailTemplates.GetFollowUpScheduledTemplate(patientName, doctorName, followUpDate);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(new MailboxAddress(patientName, patientEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = body };

            // Generate and attach the .ics calendar invite
            var icsContent = GenerateFollowUpIcsContent(appointmentId, doctorName, followUpDate, durationMinutes);
            var calendarBytes = System.Text.Encoding.UTF8.GetBytes(icsContent);
            bodyBuilder.Attachments.Add("follow-up-appointment.ics", calendarBytes, new ContentType("text", "calendar"));

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port, _settings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Follow-up confirmation email sent to {Email}", patientEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send follow-up confirmation email to {Email}", patientEmail);
        }
    }

    private string GenerateFollowUpIcsContent(Guid appointmentId, string doctorName, DateTime followUpDate, int durationMinutes)
    {
        var startUtc = followUpDate.ToUniversalTime();
        var endUtc = startUtc.AddMinutes(durationMinutes);
        var start = startUtc.ToString("yyyyMMddTHHmmssZ");
        var end = endUtc.ToString("yyyyMMddTHHmmssZ");
        var now = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");

        return $@"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//SecureMedicalRecordSystem//FollowUp//EN
BEGIN:VEVENT
UID:{appointmentId}
DTSTAMP:{now}
DTSTART:{start}
DTEND:{end}
SUMMARY:Follow-Up Appointment with Dr. {doctorName}
DESCRIPTION:This is a follow-up appointment scheduled by your doctor.
LOCATION:Medical Center
END:VEVENT
END:VCALENDAR";
    }
}
