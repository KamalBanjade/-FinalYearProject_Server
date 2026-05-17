using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SecureMedicalRecordSystem.Core.Interfaces;

namespace SecureMedicalRecordSystem.Infrastructure.BackgroundJobs;

public class AppointmentStatusWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AppointmentStatusWorker> _logger;

    // Normal polling interval
    private static readonly TimeSpan NormalInterval = TimeSpan.FromMinutes(5);

    // Backoff caps: 10s → 30s → 60s → 120s → 300s (5 min max)
    private static readonly TimeSpan[] BackoffIntervals =
    [
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(60),
        TimeSpan.FromSeconds(120),
        TimeSpan.FromSeconds(300),
    ];

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

        int consecutiveFailures = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessTransitionsAsync(stoppingToken);

                // Reset backoff on success
                if (consecutiveFailures > 0)
                {
                    _logger.LogInformation("Appointment Status Worker recovered after {Count} failure(s).", consecutiveFailures);
                    consecutiveFailures = 0;
                }

                await Task.Delay(NormalInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown — don't log as error
                break;
            }
            catch (Exception ex)
            {
                consecutiveFailures++;
                var backoff = BackoffIntervals[Math.Min(consecutiveFailures - 1, BackoffIntervals.Length - 1)];

                _logger.LogError(ex,
                    "Error occurred while transitioning appointment statuses (attempt #{Count}). Retrying in {Delay}s.",
                    consecutiveFailures, backoff.TotalSeconds);

                try
                {
                    await Task.Delay(backoff, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
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
