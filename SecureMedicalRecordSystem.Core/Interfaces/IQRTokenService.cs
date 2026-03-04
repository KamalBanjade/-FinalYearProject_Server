using SecureMedicalRecordSystem.Core.Entities;

namespace SecureMedicalRecordSystem.Core.Interfaces;

/// <summary>
/// Service for generating and managing QR tokens for medical record access.
/// Supports both Normal (authenticated) and Emergency (unauthenticated critical data) modes.
/// </summary>
public interface IQRTokenService
{
    /// <summary>
    /// Generates a secure token for normal access to patient records.
    /// Requires secondary authentication (e.g. TOTP) to view full records.
    /// </summary>
    Task<(string Token, DateTime ExpiresAt)> GenerateNormalAccessTokenAsync(Guid patientId, int expiryDays = 30);

    /// <summary>
    /// Generates a secure token for emergency access (bypass mode).
    /// Provides access to only critical medical information without authentication.
    /// </summary>
    Task<(string Token, DateTime ExpiresAt)> GenerateEmergencyAccessTokenAsync(Guid patientId, int expiryDays = 365);

    /// <summary>
    /// Validates a token string, checks expiration and activity status.
    /// Updates access tracking metadata.
    /// </summary>
    Task<(bool IsValid, QRToken? TokenData)> ValidateTokenAsync(string token);

    /// <summary>
    /// Intentionally invalidates a token to prevent further access.
    /// </summary>
    Task<bool> RevokeTokenAsync(string token);

    /// <summary>
    /// Retrieves all active QR tokens generated for a specific patient.
    /// </summary>
    Task<List<QRToken>> GetPatientTokensAsync(Guid patientId);
}
