using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SecureMedicalRecordSystem.Infrastructure.Data;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Core.Entities;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class EmailReminderService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailReminderService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check every hour
    private readonly int _reminderThresholdHours = 24;

    public EmailReminderService(
        IServiceProvider serviceProvider,
        ILogger<EmailReminderService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email Reminder Background Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while executing setup reminders.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Email Reminder Background Service is stopping.");
    }

    private async Task SendRemindersAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var cutoffTime = DateTime.UtcNow.AddHours(-_reminderThresholdHours);

        // Find patients who signed up > 24h ago, haven't completed setup, and haven't been reminded
        var usersToRemind = await context.Users
            .Where(u => u.Role == "Patient" 
                     && !u.TOTPSetupCompleted 
                     && !u.TOTPReminderSent 
                     && u.CreatedAt <= cutoffTime)
            .ToListAsync(stoppingToken);

        if (usersToRemind.Any())
        {
            _logger.LogInformation("Found {Count} users requiring security setup reminders.", usersToRemind.Count);

            foreach (var user in usersToRemind)
            {
                try
                {
                    string subject = "Action Required: Complete Your Security Setup - सजिलो स्वास्थ्य";
                    string body = $@"
                        <h3>Hello {user.FirstName},</h3>
                        <p>We noticed you haven't completed your security setup yet.</p>
                        <p>To ensure your medical records are safe and to enable sharing them with your doctors, please complete the Two-Factor Authentication (2FA) setup.</p>
                        <p><strong>Steps to complete:</strong></p>
                        <ol>
                            <li>Log in to your account at सजिलो स्वास्थ्य.</li>
                            <li>Follow the prompt to scan your 2FA QR code with Google Authenticator.</li>
                            <li>Save your permanent Medical Access QR code.</li>
                        </ol>
                        <p>If you have any questions, please contact our support team.</p>
                        <p>Stay safe,<br/>The सजिलो स्वास्थ्य Team</p>";

                    await emailService.SendEmailAsync(user.Email!, subject, body);

                    user.TOTPReminderSent = true;
                    _logger.LogInformation("Reminder email sent to {Email}.", user.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send reminder email to {Email}.", user.Email);
                }
            }

            await context.SaveChangesAsync(stoppingToken);
        }
    }
}
