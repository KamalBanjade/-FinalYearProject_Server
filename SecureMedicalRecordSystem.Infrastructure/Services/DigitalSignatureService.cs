using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using SecureMedicalRecordSystem.Core.Interfaces;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class DigitalSignatureService : IDigitalSignatureService
{
    private readonly IKeyManagementService _keyManagementService;
    private readonly ILogger<DigitalSignatureService> _logger;

    public DigitalSignatureService(IKeyManagementService keyManagementService, ILogger<DigitalSignatureService> logger)
    {
        _keyManagementService = keyManagementService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<string> SignDataAsync(string dataToSign, string encryptedPrivateKey)
    {
        _logger.LogInformation("Signing data using RSA-SHA256.");

        try
        {
            // 1. Decrypt doctor's private key
            var privateKeyPem = await _keyManagementService.DecryptPrivateKey(encryptedPrivateKey);

            // 2. Import into RSA
            using var rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPem);

            // 3. Convert data to bytes
            var dataBytes = Encoding.UTF8.GetBytes(dataToSign);

            // 4. Sign using RSA-SHA256 (RSASignaturePadding.Pkcs1 is standard)
            var signatureBytes = rsa.SignData(
                dataBytes, 
                HashAlgorithmName.SHA256, 
                RSASignaturePadding.Pkcs1);

            return Convert.ToBase64String(signatureBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during digital signing.");
            throw new CryptographicException("Failed to sign record data.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> VerifySignatureAsync(string data, string signature, string publicKey)
    {
        _logger.LogInformation("Verifying RSA-SHA256 signature.");

        try
        {
            // 1. Import public key
            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKey);

            // 2. Convert data and signature to bytes
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signatureBytes = Convert.FromBase64String(signature);

            // 3. Verify
            var isValid = rsa.VerifyData(
                dataBytes,
                signatureBytes,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            if (isValid)
            {
                _logger.LogInformation("Signature verification successful.");
            }
            else
            {
                _logger.LogWarning("Signature verification FAILED. Data may have been tampered with.");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during signature verification.");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<string> ComputeRecordHashAsync(Stream fileStream)
    {
        // Reset stream position if possible
        if (fileStream.CanSeek)
            fileStream.Position = 0;

        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(fileStream);

        // Reset positions again for subsequent use
        if (fileStream.CanSeek)
            fileStream.Position = 0;

        return Convert.ToBase64String(hashBytes);
    }
}
