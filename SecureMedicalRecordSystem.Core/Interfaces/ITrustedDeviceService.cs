using SecureMedicalRecordSystem.Core.DTOs.Auth;
using SecureMedicalRecordSystem.Core.Entities;

namespace SecureMedicalRecordSystem.Core.Interfaces;

public interface ITrustedDeviceService
{
    Task<string?> GetDeviceTokenFromRequest(string? cookieDeviceToken);
    Task<bool> IsDeviceTrustedAsync(string deviceToken, Guid userId);
    Task<string> CreateTrustedDeviceAsync(Guid userId, string userAgent, string acceptLanguage, string ipAddress, int expiryDays = 30);
    Task<List<TrustedDeviceDTO>> GetUserTrustedDevicesAsync(Guid userId);
    Task<bool> RevokeDeviceAsync(Guid deviceId, Guid userId, string reason = "User revoked");
    Task RevokeAllUserDevicesAsync(Guid userId, string reason = "User revoked all");
    Task CleanupExpiredDevicesAsync();
}
