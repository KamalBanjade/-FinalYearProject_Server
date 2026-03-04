using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Infrastructure.Configuration;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

/// <summary>
/// AES-256-CBC encryption service implementing both byte-array and stream-based operations.
/// Task 5: EncryptFileAsync, DecryptFileAsync, ComputeFileHashAsync added.
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;
    private readonly ILogger<EncryptionService> _logger;

    public EncryptionService(IOptions<EncryptionSettings> settings, ILogger<EncryptionService> logger)
    {
        _logger = logger;
        _key = Convert.FromBase64String(settings.Value.AESKey);
        _iv = Convert.FromBase64String(settings.Value.AESIV);

        if (_key.Length != 32)
            throw new InvalidOperationException("AES key must be 32 bytes (256 bits).");
        if (_iv.Length != 16)
            throw new InvalidOperationException("AES IV must be 16 bytes (128 bits).");
    }

    // =========================================================
    // String / byte-array methods (field-level encryption)
    // =========================================================

    /// <inheritdoc/>
    public string Encrypt(string plaintext)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        return Convert.ToBase64String(EncryptBytes(plaintextBytes));
    }

    /// <inheritdoc/>
    public string Decrypt(string ciphertext)
    {
        var ciphertextBytes = Convert.FromBase64String(ciphertext);
        return Encoding.UTF8.GetString(DecryptBytes(ciphertextBytes));
    }

    /// <inheritdoc/>
    public byte[] EncryptBytes(byte[] data)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(data, 0, data.Length);
        }
        return ms.ToArray();
    }

    /// <inheritdoc/>
    public byte[] DecryptBytes(byte[] data)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(data);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var output = new MemoryStream();
        cs.CopyTo(output);
        return output.ToArray();
    }

    /// <inheritdoc/>
    public string ComputeHash(string input)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Computes SHA-256 of raw bytes (for file integrity — used by MedicalRecordService).
    /// </summary>
    public string ComputeHashFromBytes(byte[] data)
    {
        return Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
    }

    // =========================================================
    // TASK 5 - Stream-based methods (file encryption)
    // =========================================================

    /// <inheritdoc/>
    public async Task<Stream> EncryptFileAsync(Stream inputStream)
    {
        _logger.LogDebug("Encrypting file stream using AES-256-CBC.");

        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            var encryptor = aes.CreateEncryptor();
            var outputStream = new MemoryStream();

            await using (var cryptoStream = new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write, leaveOpen: true))
            {
                await inputStream.CopyToAsync(cryptoStream);
                await cryptoStream.FlushFinalBlockAsync();
            }

            outputStream.Position = 0;
            _logger.LogDebug("File stream encrypted successfully ({Bytes} bytes ciphertext).", outputStream.Length);
            return outputStream;
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Cryptographic error during file encryption.");
            throw new InvalidOperationException("File encryption failed. Ensure the AES key and IV are valid.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Stream> DecryptFileAsync(Stream encryptedStream)
    {
        _logger.LogDebug("Decrypting file stream using AES-256-CBC.");

        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            var decryptor = aes.CreateDecryptor();
            var outputStream = new MemoryStream();

            await using (var cryptoStream = new CryptoStream(encryptedStream, decryptor, CryptoStreamMode.Read, leaveOpen: true))
            {
                await cryptoStream.CopyToAsync(outputStream);
            }

            outputStream.Position = 0;
            _logger.LogDebug("File stream decrypted successfully ({Bytes} bytes plaintext).", outputStream.Length);
            return outputStream;
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Cryptographic error during file decryption. File may have been tampered with.");
            throw new InvalidOperationException("File decryption failed. The file may be corrupted or tampered with.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<string> ComputeFileHashAsync(Stream fileStream)
    {
        // Reset stream to beginning before hashing
        if (fileStream.CanSeek)
            fileStream.Position = 0;

        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(fileStream);

        // Reset stream position for subsequent use
        if (fileStream.CanSeek)
            fileStream.Position = 0;

        var hashString = Convert.ToBase64String(hashBytes);
        _logger.LogDebug("File hash computed: {Hash}", hashString[..8] + "...");
        return hashString;
    }
}
