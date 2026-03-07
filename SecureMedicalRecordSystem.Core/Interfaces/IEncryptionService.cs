namespace SecureMedicalRecordSystem.Core.Interfaces;

/// <summary>
/// AES-256-CBC encryption service for field-level and file (stream-based) encryption.
/// </summary>
public interface IEncryptionService
{
    // --- String / byte-array methods (used for field-level encryption) ---

    /// <summary>Encrypts a UTF-8 string and returns Base64-encoded ciphertext.</summary>
    string Encrypt(string plaintext);

    /// <summary>Decrypts a Base64-encoded ciphertext string and returns the original UTF-8 string.</summary>
    string Decrypt(string ciphertext);

    /// <summary>Encrypts raw bytes using AES-256-CBC.</summary>
    byte[] EncryptBytes(byte[] data);

    /// <summary>Decrypts raw bytes using AES-256-CBC.</summary>
    byte[] DecryptBytes(byte[] data);

    /// <summary>Computes a SHA-256 hex hash of the given UTF-8 string.</summary>
    string ComputeHash(string input);

    // --- Stream-based methods (used for file encryption) ---

    /// <summary>
    /// Encrypts an input stream using AES-256-CBC and returns the encrypted stream.
    /// Suitable for large file streaming without loading the entire file into memory.
    /// </summary>
    Task<Stream> EncryptFileAsync(Stream inputStream);

    /// <summary>
    /// Decrypts an AES-256-CBC encrypted stream and returns the decrypted stream.
    /// </summary>
    /// <summary>
    /// Computes a SHA-256 hash of a file stream and returns a Base64 string.
    /// Resets stream position before and after hashing.
    /// Used for integrity verification.
    /// </summary>
    Task<string> ComputeFileHashAsync(Stream fileStream);

    /// <summary>
    /// Returns a live CryptoStream wrapping the provided encrypted stream using AES-256-CBC.
    /// Does NOT buffer — designed for true end-to-end pipelined streaming.
    /// The caller must dispose the returned stream when done.
    /// </summary>
    Stream CreateDecryptingStream(Stream encryptedStream);
}
