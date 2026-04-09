using Microsoft.AspNetCore.SignalR;
using SecureMedicalRecordSystem.API.Hubs;
using SecureMedicalRecordSystem.Core.DTOs.Analysis;
using SecureMedicalRecordSystem.Core.Interfaces;

namespace SecureMedicalRecordSystem.API.Services;

public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendStabilityAlertAsync(Guid doctorId, StabilityAlertDto alert)
    {
        await _hubContext.Clients
            .User(doctorId.ToString())
            .SendAsync("ReceiveStabilityAlert", alert);
    }
}
