using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SecureMedicalRecordSystem.Core.Interfaces;

namespace SecureMedicalRecordSystem.API.BackgroundServices;

public class TrustedDeviceCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TrustedDeviceCleanupService> _logger;

    public TrustedDeviceCleanupService(
        IServiceProvider serviceProvider,
        ILogger<TrustedDeviceCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Run cleanup daily
                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ITrustedDeviceService>();
                
                await service.CleanupExpiredDevicesAsync();
                
                _logger.LogInformation("Trusted device cleanup completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during trusted device cleanup");
            }

            // Wait 24 hours
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
