namespace SecureMedicalRecordSystem.Core.DTOs.Auth;

public class TrustedDeviceDTO
{
    public Guid Id { get; set; }
    public string DeviceToken { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string IPAddress { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public int DaysUntilExpiry { get; set; }
    public bool IsCurrentDevice { get; set; }
}
