namespace SecureMedicalRecordSystem.Infrastructure.Configuration;

public class MasterKeySettings
{
    /// <summary>
    /// 256-bit (32 bytes) Base64-encoded key used to encrypt doctor private keys.
    /// </summary>
    public string RsaPrivateKeyEncryptionKey { get; set; } = string.Empty;
}
