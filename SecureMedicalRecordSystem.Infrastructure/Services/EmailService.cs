using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
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
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true);
            }
        }
    }
}
