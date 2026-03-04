using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SecureMedicalRecordSystem.Core.Interfaces;

namespace SecureMedicalRecordSystem.Infrastructure.BackgroundJobs;

public class AccessSessionCleanupWorker(
    IServiceProvider serviceProvider,
    ILogger<AccessSessionCleanupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Access Session Cleanup Worker is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    var sessionService = scope.ServiceProvider.GetRequiredService<IAccessSessionService>();
                    await sessionService.CleanupExpiredSessionsAsync();
                }

                // Run every 1 hour
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Prevent exception when stopping
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while cleaning up expired access sessions.");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Retry sooner on error
            }
        }

        logger.LogInformation("Access Session Cleanup Worker is stopping.");
    }
}
