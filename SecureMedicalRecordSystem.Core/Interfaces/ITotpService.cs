namespace SecureMedicalRecordSystem.Core.Interfaces;

/// <summary>
/// TOTP (Time-based One-Time Password) service for 2FA via authenticator apps.
/// </summary>
public interface ITotpService
{
    string GenerateSecret();
    string GenerateQrCodeUri(string secret, string email, string issuer = "SecureMedicalSystem");
    bool ValidateTotp(string secret, string code);
    byte[] GenerateQrCodeImage(string qrCodeUri);  // Returns PNG bytes
}
