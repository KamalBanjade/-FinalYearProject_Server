namespace SecureMedicalRecordSystem.Infrastructure.Configuration;

/// <summary>
/// AES-256 encryption key configuration. Keys are Base64-encoded.
/// IMPORTANT: Never commit actual keys. Use environment variables in production.
/// </summary>
public class EncryptionSettings
{
    public string AESKey { get; set; } = string.Empty;   // Base64-encoded 32 bytes (256-bit)
    public string AESIV { get; set; } = string.Empty;    // Base64-encoded 16 bytes (128-bit)
}
