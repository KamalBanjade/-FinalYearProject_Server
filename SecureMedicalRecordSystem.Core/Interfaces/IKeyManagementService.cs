using System;
using System.Threading.Tasks;

namespace SecureMedicalRecordSystem.Core.Interfaces;

public interface IKeyManagementService
{
    /// <summary>
    /// Generates a new RSA-2048 key pair.
    /// The private key is returned in an encrypted format using the system's master key.
    /// Both keys are PEM-formatted strings.
    /// </summary>
    Task<(string PublicKey, string EncryptedPrivateKey)> GenerateRsaKeyPairAsync();

    /// <summary>
    /// Encrypts a PEM-formatted private key using the AES-256 master key.
    /// </summary>
    string EncryptPrivateKey(string privateKeyPem);

    /// <summary>
    /// Decrypts an encrypted private key for signing operations.
    /// Returns the PEM-formatted private key.
    /// </summary>
    Task<string> DecryptPrivateKey(string encryptedPrivateKey);

    /// <summary>
    /// Validates that a public key and encrypted private key form a mathematically correct pair.
    /// </summary>
    Task<bool> ValidateKeyPairAsync(string publicKey, string encryptedPrivateKey);

    /// <summary>
    /// Generates a new key pair for a doctor and returns it.
    /// </summary>
    Task<(string PublicKey, string EncryptedPrivateKey)> RotateKeyPairAsync(Guid doctorId);
}
