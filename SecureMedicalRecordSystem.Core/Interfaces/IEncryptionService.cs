namespace SecureMedicalRecordSystem.Core.Interfaces;

/// <summary>
/// AES-256 encryption service for field-level and file encryption.
/// </summary>
public interface IEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
    byte[] EncryptBytes(byte[] data);
    byte[] DecryptBytes(byte[] data);
    string ComputeHash(string input);  // SHA-256
}
