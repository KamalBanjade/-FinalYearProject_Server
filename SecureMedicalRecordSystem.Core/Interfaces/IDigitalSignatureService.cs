using System.IO;
using System.Threading.Tasks;

namespace SecureMedicalRecordSystem.Core.Interfaces;

public interface IDigitalSignatureService
{
    /// <summary>
    /// Signs a piece of data (typically a record hash) with the doctor's encrypted private key.
    /// Returns the Base64-encoded signature.
    /// </summary>
    Task<string> SignDataAsync(string dataToSign, string encryptedPrivateKey);

    /// <summary>
    /// Verifies a signature against the provided data and doctor's public key.
    /// Returns true if the signature is valid.
    /// </summary>
    Task<bool> VerifySignatureAsync(string data, string signature, string publicKey);

    /// <summary>
    /// Computes a secure SHA-256 hash of a medical record file stream.
    /// </summary>
    Task<string> ComputeRecordHashAsync(Stream fileStream);
}
