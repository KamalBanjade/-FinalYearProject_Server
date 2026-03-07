namespace SecureMedicalRecordSystem.Core.Interfaces;

/// <summary>
/// Tigris S3-compatible storage service for encrypted medical file management.
/// </summary>
public interface ITigrisStorageService
{
    /// <summary>
    /// Ensures the configured bucket exists, creating it if necessary.
    /// Should be called on app startup.
    /// </summary>
    Task InitializeBucketAsync();

    /// <summary>
    /// Uploads an already-encrypted file stream to Tigris.
    /// </summary>
    /// <param name="fileStream">The encrypted file stream to upload.</param>
    /// <param name="objectKey">The S3 key to store the file under (e.g. records/{patientId}/{guid}.enc).</param>
    /// <param name="contentType">The MIME type of the original file.</param>
    /// <param name="originalFileName">Original filename stored as object metadata.</param>
    /// <returns>The S3 object key used to store the file.</returns>
    Task<string> UploadFileAsync(Stream fileStream, string objectKey, string contentType, string originalFileName);

    /// <summary>
    /// Downloads an encrypted file stream from Tigris by its object key.
    /// Buffers into MemoryStream for safe disposal. Use OpenDownloadStreamAsync for streaming.
    /// </summary>
    /// <returns>A readable stream of the encrypted file bytes.</returns>
    Task<Stream> DownloadFileAsync(string objectKey);

    /// <summary>
    /// Opens a raw S3 response stream for direct pipelined streaming without buffering.
    /// The caller must dispose the returned stream (which disposes the S3 response).
    /// Use for View/Download endpoints where pipelined streaming is required.
    /// </summary>
    Task<Stream> OpenDownloadStreamAsync(string objectKey);

    /// <summary>
    /// Deletes a file from Tigris storage.
    /// </summary>
    /// <returns>True if deleted, false on error.</returns>
    Task<bool> DeleteFileAsync(string objectKey);

    /// <summary>
    /// Checks whether a file exists in Tigris storage.
    /// </summary>
    Task<bool> FileExistsAsync(string objectKey);

    /// <summary>
    /// Gets the size in bytes of a stored file.
    /// </summary>
    Task<long> GetFileSizeAsync(string objectKey);

    // --- Legacy byte-array convenience methods (used internally) ---

    /// <summary>Uploads encrypted bytes directly (convenience wrapper).</summary>
    Task<string> UploadEncryptedFileAsync(byte[] encryptedData, string objectKey, string contentType);

    /// <summary>Downloads as byte array (convenience wrapper).</summary>
    Task<byte[]> DownloadEncryptedFileAsync(string objectKey);

    /// <summary>Checks whether the configured bucket exists.</summary>
    Task<bool> BucketExistsAsync();
}
