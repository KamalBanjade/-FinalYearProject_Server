using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SecureMedicalRecordSystem.Core.Interfaces;

namespace SecureMedicalRecordSystem.Infrastructure.BackgroundJobs;

public class AppointmentStatusWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AppointmentStatusWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // Run every 5 minutes

    public AppointmentStatusWorker(
        IServiceProvider serviceProvider,
        ILogger<AppointmentStatusWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Appointment Status Worker is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessTransitionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while transitioning appointment statuses.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Appointment Status Worker is stopping.");
    }

    private async Task ProcessTransitionsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var appointmentService = scope.ServiceProvider.GetRequiredService<IAppointmentService>();

        _logger.LogDebug("Running scheduled appointment status transitions...");
        var processedCount = await appointmentService.CheckAndTransitionAppointmentStatusesAsync();
        
        if (processedCount > 0)
        {
            _logger.LogInformation("Successfully processed {Count} appointment status transitions.", processedCount);
        }
    }
}
