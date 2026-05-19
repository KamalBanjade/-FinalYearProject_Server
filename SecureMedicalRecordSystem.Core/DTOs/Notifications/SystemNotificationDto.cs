using System;

namespace SecureMedicalRecordSystem.Core.DTOs.Notifications;

public class SystemNotificationDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "General";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ReferenceId { get; set; }
    public bool IsRead { get; set; } = false;
}
