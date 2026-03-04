using SecureMedicalRecordSystem.Core.DTOs.QR;

namespace SecureMedicalRecordSystem.Core.Interfaces;

/// <summary>
/// Service for managing temporary access sessions for medical records.
/// Created via QR token scans and validated for short-term access.
/// </summary>
public interface IAccessSessionService
{
    /// <summary>
    /// Creates a temporary access session after validating the QR token and optional TOTP code.
    /// </summary>
    Task<(bool Success, string Message, AccessSessionDTO? Session)> CreateSessionAsync(
        string token, 
        string? totpCode,
        string ipAddress,
        string userAgent);

    /// <summary>
    /// Validates an active access session by its unique session token.
    /// </summary>
    Task<(bool Success, AccessSessionDTO? Session)> ValidateSessionAsync(string sessionId);

    /// <summary>
    /// Deactivates an active session (manual logout).
    /// </summary>
    Task<bool> RevokeSessionAsync(string sessionId);

    /// <summary>
    /// Background task to clean up expired sessions from the database.
    /// </summary>
    Task CleanupExpiredSessionsAsync();
}
