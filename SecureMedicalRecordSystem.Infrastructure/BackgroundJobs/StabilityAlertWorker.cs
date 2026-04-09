using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Core.Settings;

namespace SecureMedicalRecordSystem.Infrastructure.BackgroundJobs;

public class StabilityAlertWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<AnalysisSettings> _settings;

    public StabilityAlertWorker(
        IServiceProvider serviceProvider,
        IOptions<AnalysisSettings> settings)
    {
        _serviceProvider = serviceProvider;
        _settings = settings;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var alertService = scope
                    .ServiceProvider
                    .GetRequiredService<IStabilityAlertService>();

                await alertService.CheckAndTriggerAlertsAsync(stoppingToken);
            }
            catch (Exception)
            {
                // Worker must never crash — swallow top-level exceptions
            }

            await Task.Delay(
                TimeSpan.FromMinutes(_settings.Value.StabilityWorkerIntervalMinutes),
                stoppingToken);
        }
    }
}
