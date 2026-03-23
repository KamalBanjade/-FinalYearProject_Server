namespace SecureMedicalRecordSystem.Core.Interfaces;

/// <summary>
/// Provides the base URLs for the frontend and backend at runtime.
/// Values come from configuration (appsettings.Development.json) when explicitly set,
/// or are auto-detected from the machine's active local-network IPv4 address.
/// </summary>
public interface ILocalUrlProvider
{
    /// <summary>
    /// Resolved frontend base URL (defaulting to localhost:3000).
    /// </summary>
    string FrontendBaseUrl { get; }

    /// <summary>
    /// Resolved frontend base URL using local IP (e.g. http://192.168.1.70:3000).
    /// Used for QR codes and external device access.
    /// </summary>
    string FrontendIpBaseUrl { get; }

    /// <summary>
    /// Resolved backend/API base URL. Example: http://192.168.1.8:5004
    /// </summary>
    string BackendBaseUrl { get; }

    /// <summary>
    /// The raw detected local IPv4 address (or "127.0.0.1" if none found).
    /// </summary>
    string LocalIpAddress { get; }

    /// <summary>
    /// Resolved email confirmation link template.
    /// Example: http://192.168.1.8:3000/confirm-email?token=[TOKEN]&userId=[USERID]
    /// </summary>
    string EmailConfirmationLinkTemplate { get; }

    /// <summary>
    /// Resolved password reset link template.
    /// Example: http://192.168.1.8:3000/reset-password?token=[TOKEN]&userId=[USERID]
    /// </summary>
    string PasswordResetLinkTemplate { get; }
}
