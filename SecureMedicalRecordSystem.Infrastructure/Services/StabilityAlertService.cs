using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecureMedicalRecordSystem.Core.DTOs.Analysis;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Core.Settings;
using SecureMedicalRecordSystem.Infrastructure.Data;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class StabilityAlertService : IStabilityAlertService
{
    private readonly ApplicationDbContext _context;
    private readonly IHealthAnalysisService _analysisService;
    private readonly INotificationService _notificationService;
    private readonly IOptions<AnalysisSettings> _settings;
    private readonly ILogger<StabilityAlertService> _logger;

    public StabilityAlertService(
        ApplicationDbContext context,
        IHealthAnalysisService analysisService,
        INotificationService notificationService,
        IOptions<AnalysisSettings> settings,
        ILogger<StabilityAlertService> logger)
    {
        _context = context;
        _analysisService = analysisService;
        _notificationService = notificationService;
        _settings = settings;
        _logger = logger;
    }

    public async Task CheckAndTriggerAlertsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking for stability alerts...");
        var patients = await _context.Patients
            .Where(p => p.PrimaryDoctorId != null && !p.IsDeleted)
            .Select(p => new { p.Id, p.PrimaryDoctorId, p.User.FirstName, p.User.LastName })
            .ToListAsync(cancellationToken);

        int alertCount = 0;
        foreach (var patient in patients)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var timeline = await _analysisService.GetStabilityTimelineAsync(patient.Id);
                if (timeline.Quarters.Count == 0) continue;

                var latestQuarter = timeline.Quarters.Last();

                if (latestQuarter.StabilityScore < _settings.Value.StabilityAlertThreshold)
                {
                    bool triggered = await TriggerAlertIfNotRecentAsync(patient.Id,
                        patient.PrimaryDoctorId!.Value,
                        patient.FirstName + " " + patient.LastName,
                        latestQuarter.Quarter,
                        latestQuarter.StabilityScore,
                        latestQuarter.ScoreInterpretation,
                        cancellationToken);
                    
                    if (triggered) alertCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing stability alert for patient {PatientId}", patient.Id);
                continue;
            }
        }
        
        if (alertCount > 0)
        {
            _logger.LogInformation("Stability alert check complete. Triggered {Count} new alerts.", alertCount);
        }
    }

    private async Task<bool> TriggerAlertIfNotRecentAsync(
        Guid patientId,
        Guid doctorId,
        string patientName,
        string quarter,
        double score,
        string interpretation,
        CancellationToken cancellationToken)
    {
        var recentExists = await _context.StabilityAlerts.AnyAsync(a =>
            a.PatientId == patientId &&
            a.Quarter == quarter &&
            a.TriggeredAt > DateTime.UtcNow.AddHours(-24) &&
            !a.IsDeleted,
            cancellationToken);

        if (recentExists) return false;

        // Persist alert
        var alert = new StabilityAlert
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DoctorId = doctorId,
            TriggeredAt = DateTime.UtcNow,
            Quarter = quarter,
            StabilityScore = score,
            ScoreInterpretation = interpretation,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system-alert-worker"
        };
        _context.StabilityAlerts.Add(alert);
        await _context.SaveChangesAsync(cancellationToken);

        // Push real-time notification via abstracted service
        var payload = new StabilityAlertDto
        {
            AlertId = alert.Id,
            PatientId = patientId,
            PatientName = patientName,
            Quarter = quarter,
            StabilityScore = score,
            ScoreInterpretation = interpretation,
            TriggeredAt = alert.TriggeredAt,
            IsRead = false
        };

        await _notificationService.SendStabilityAlertAsync(doctorId, payload);
        return true;
    }

    public async Task<List<StabilityAlertDto>> GetUnreadAlertsForDoctorAsync(Guid doctorId)
    {
        return await _context.StabilityAlerts
            .Include(a => a.Patient)
            .ThenInclude(p => p.User)
            .Where(a => a.DoctorId == doctorId && !a.IsRead && !a.IsDeleted)
            .OrderByDescending(a => a.TriggeredAt)
            .Select(a => new StabilityAlertDto
            {
                AlertId = a.Id,
                PatientId = a.PatientId,
                PatientName = a.Patient.User.FirstName + " " + a.Patient.User.LastName,
                Quarter = a.Quarter,
                StabilityScore = a.StabilityScore,
                ScoreInterpretation = a.ScoreInterpretation,
                TriggeredAt = a.TriggeredAt,
                IsRead = a.IsRead
            })
            .ToListAsync();
    }

    public async Task MarkAlertAsReadAsync(Guid alertId, Guid doctorId)
    {
        var alert = await _context.StabilityAlerts
            .FirstOrDefaultAsync(a => a.Id == alertId &&
                                      a.DoctorId == doctorId &&
                                      !a.IsDeleted)
            ?? throw new KeyNotFoundException("Alert not found.");

        alert.IsRead = true;
        alert.ReadAt = DateTime.UtcNow;
        alert.UpdatedAt = DateTime.UtcNow;
        alert.UpdatedBy = doctorId.ToString();
        await _context.SaveChangesAsync();
    }

    public async Task<int> GetUnreadAlertCountAsync(Guid doctorId)
    {
        return await _context.StabilityAlerts
            .CountAsync(a => a.DoctorId == doctorId && !a.IsRead && !a.IsDeleted);
    }
}
