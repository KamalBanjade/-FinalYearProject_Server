using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecureMedicalRecordSystem.Core.DTOs.Analysis;
using SecureMedicalRecordSystem.Core.DTOs.Notifications;

namespace SecureMedicalRecordSystem.Core.Interfaces;

public interface INotificationService
{
    Task SendStabilityAlertAsync(Guid doctorId, StabilityAlertDto alert);
    Task SendNotificationAsync(Guid userId, SystemNotificationDto notification);
    /// <summary>Persists the notification to the database then pushes it live via SignalR.</summary>
    Task PersistAndSendNotificationAsync(Guid userId, SystemNotificationDto notification);
    Task<List<SystemNotificationDto>> GetRecentNotificationsAsync(Guid userId, int count = 30);
    Task MarkNotificationAsReadAsync(Guid notificationId, Guid userId);
    Task MarkAllNotificationsAsReadAsync(Guid userId);
}

