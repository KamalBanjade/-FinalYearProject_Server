using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Infrastructure.Configuration;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class KeyManagementService : IKeyManagementService
{
    private readonly byte[] _masterKeyBytes;
    private readonly ILogger<KeyManagementService> _logger;

    public KeyManagementService(IOptions<MasterKeySettings> settings, ILogger<KeyManagementService> logger)
    {
        _logger = logger;
        var masterKeyBase64 = settings.Value.RsaPrivateKeyEncryptionKey;
        
        if (string.IsNullOrEmpty(masterKeyBase64))
        {
            _logger.LogCritical("RSA Master Key is missing from configuration.");
            throw new InvalidOperationException("RSA Master Key is not configured.");
        }

        try
        {
            _masterKeyBytes = Convert.FromBase64String(masterKeyBase64);
            if (_masterKeyBytes.Length != 32)
            {
                throw new InvalidOperationException("RSA Master Key must be 32 bytes (256-bit).");
            }
        }
        catch (FormatException ex)
        {
            _logger.LogCritical(ex, "RSA Master Key is not a valid Base64 string.");
            throw new InvalidOperationException("Invalid RSA Master Key format.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<(string PublicKey, string EncryptedPrivateKey)> GenerateRsaKeyPairAsync()
    {
        _logger.LogInformation("Generating new RSA-2048 key pair.");

        using var rsa = RSA.Create(2048);
        
        var privateKeyPem = rsa.ExportRSAPrivateKeyPem();
        var publicKeyPem = rsa.ExportRSAPublicKeyPem();

        var encryptedPrivateKey = EncryptPrivateKey(privateKeyPem);

        return await Task.FromResult((publicKeyPem, encryptedPrivateKey));
    }

    /// <inheritdoc/>
    public string EncryptPrivateKey(string privateKeyPem)
    {
        var privateKeyBytes = Encoding.UTF8.GetBytes(privateKeyPem);

        using var aes = Aes.Create();
        aes.Key = _masterKeyBytes;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        
        aes.GenerateIV();
        var iv = aes.IV;

        using var encryptor = aes.CreateEncryptor();
        using var msEncrypt = new MemoryStream();
        
        // Write IV first
        msEncrypt.Write(iv, 0, iv.Length);

        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        {
            csEncrypt.Write(privateKeyBytes, 0, privateKeyBytes.Length);
            csEncrypt.FlushFinalBlock();
        }

        var results = msEncrypt.ToArray();
        return Convert.ToBase64String(results);
    }

    /// <inheritdoc/>
    public async Task<string> DecryptPrivateKey(string encryptedPrivateKey)
    {
        var combinedBytes = Convert.FromBase64String(encryptedPrivateKey);

        using var aes = Aes.Create();
        aes.Key = _masterKeyBytes;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        // Extract IV
        var iv = new byte[16];
        Buffer.BlockCopy(combinedBytes, 0, iv, 0, 16);
        aes.IV = iv;

        // Extract ciphertext
        var encryptedBytes = new byte[combinedBytes.Length - 16];
        Buffer.BlockCopy(combinedBytes, 16, encryptedBytes, 0, encryptedBytes.Length);

        using var decryptor = aes.CreateDecryptor();
        using var msDecrypt = new MemoryStream(encryptedBytes);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);

        return await srDecrypt.ReadToEndAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateKeyPairAsync(string publicKey, string encryptedPrivateKey)
    {
        try
        {
            var privateKeyPem = await DecryptPrivateKey(encryptedPrivateKey);

            using var rsaPrivate = RSA.Create();
            rsaPrivate.ImportFromPem(privateKeyPem);

            using var rsaPublic = RSA.Create();
            rsaPublic.ImportFromPem(publicKey);

            var testData = Encoding.UTF8.GetBytes("Integrity-Check-" + Guid.NewGuid());
            
            // Encrypt with public
            var encrypted = rsaPublic.Encrypt(testData, RSAEncryptionPadding.OaepSHA256);
            
            // Decrypt with private
            var decrypted = rsaPrivate.Decrypt(encrypted, RSAEncryptionPadding.OaepSHA256);

            return testData.SequenceEqual(decrypted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Key pair validation failed.");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<(string PublicKey, string EncryptedPrivateKey)> RotateKeyPairAsync(Guid doctorId)
    {
        _logger.LogWarning("Rotating key pair for doctor: {DoctorId}", doctorId);
        return await GenerateRsaKeyPairAsync();
    }
}
