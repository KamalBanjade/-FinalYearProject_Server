using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Infrastructure.Configuration;
using System.Net;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

/// <summary>
/// Tigris S3-compatible storage service for encrypted medical file management.
/// Implements both Stream-based (Task 4) and byte-array (legacy) methods.
/// </summary>
public class TigrisStorageService : ITigrisStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly ILogger<TigrisStorageService> _logger;

    public TigrisStorageService(IAmazonS3 s3Client, IOptions<TigrisSettings> settings, ILogger<TigrisStorageService> logger)
    {
        _s3Client = s3Client;
        _logger = logger;
        _bucketName = settings.Value.BucketName;
    }

    // =========================================================
    // TASK 4 - Method 1: InitializeBucketAsync
    // =========================================================

    /// <inheritdoc/>
    public async Task InitializeBucketAsync()
    {
        try
        {
            var listResponse = await _s3Client.ListBucketsAsync();
            bool bucketExists = listResponse.Buckets.Any(b => b.BucketName == _bucketName);

            if (!bucketExists)
            {
                _logger.LogWarning("Bucket '{Bucket}' not found. Creating now...", _bucketName);
                await _s3Client.PutBucketAsync(new PutBucketRequest
                {
                    BucketName = _bucketName,
                    UseClientRegion = true
                });
                _logger.LogInformation("Bucket created: {Bucket}", _bucketName);
            }
            else
            {
                _logger.LogInformation("Bucket already exists: {Bucket}", _bucketName);
            }
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Error during bucket initialization for '{Bucket}': {Message}", _bucketName, ex.Message);
            throw;
        }
    }

    // =========================================================
    // TASK 4 - Method 2: UploadFileAsync (Stream-based)
    // =========================================================

    /// <inheritdoc/>
    public async Task<string> UploadFileAsync(Stream fileStream, string objectKey, string contentType, string originalFileName)
    {
        _logger.LogInformation("Uploading encrypted stream to Tigris: {ObjectKey} ({ContentType})", objectKey, contentType);

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = objectKey,
            InputStream = fileStream,
            ContentType = contentType,
            DisablePayloadSigning = true // Fix for Tigris/S3-compat: avoids 'Invalid Content-Encoding' error with chunked uploads
        };

        request.Metadata["x-amz-meta-original-filename"] = originalFileName;
        request.Metadata["x-amz-meta-upload-timestamp"] = DateTime.UtcNow.ToString("O");

        try
        {
            var response = await _s3Client.PutObjectAsync(request);

            if (response.HttpStatusCode == HttpStatusCode.OK || response.HttpStatusCode == HttpStatusCode.NoContent)
            {
                _logger.LogInformation("File uploaded to Tigris: {ObjectKey}", objectKey);
                return objectKey;
            }

            _logger.LogError("Unexpected HTTP status during upload: {Status}", response.HttpStatusCode);
            throw new InvalidOperationException($"File upload returned unexpected status: {response.HttpStatusCode}");
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "S3 error uploading {ObjectKey}: {ErrorCode} - {Message}", objectKey, ex.ErrorCode, ex.Message);
            throw;
        }
    }

    // =========================================================
    // TASK 4 - Method 3: DownloadFileAsync (Stream-based)
    // =========================================================

    /// <inheritdoc/>
    public async Task<Stream> DownloadFileAsync(string objectKey)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("[PERF] [Tigris] Starting download for {ObjectKey}", objectKey);

        var request = new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = objectKey
        };

        try
        {
            var response = await _s3Client.GetObjectAsync(request);
            var downloadStreamSw = System.Diagnostics.Stopwatch.StartNew();
            
            // Copy to MemoryStream so object response is disposed safely
            var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms);
            
            downloadStreamSw.Stop();
            _logger.LogInformation("[PERF] [Tigris] CopyToAsync (download) completed in {Ms}ms for {ObjectKey}", downloadStreamSw.ElapsedMilliseconds, objectKey);
            
            ms.Position = 0;
            sw.Stop();
            _logger.LogInformation("[PERF] [Tigris] Total DownloadFileAsync duration: {Ms}ms", sw.ElapsedMilliseconds);
            return ms;
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchKey")
        {
            _logger.LogWarning("File not found in Tigris: {ObjectKey}", objectKey);
            throw new FileNotFoundException($"File '{objectKey}' was not found in storage.", objectKey);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "S3 error downloading {ObjectKey}: {Message}", objectKey, ex.Message);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Stream> OpenDownloadStreamAsync(string objectKey)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("[PERF] [Tigris] Opening raw S3 stream (no buffer) for {ObjectKey}", objectKey);

        var request = new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = objectKey
        };

        try
        {
            // Returns the live S3 network stream. The GetObjectResponse is kept alive
            // by wrapping it in an OwningStream which disposes it after the stream is consumed.
            var response = await _s3Client.GetObjectAsync(request);
            sw.Stop();
            _logger.LogInformation("[PERF] [Tigris] S3 GetObjectAsync completed in {Ms}ms (metadata only, stream not copied)", sw.ElapsedMilliseconds);
            return new OwningS3Stream(response.ResponseStream, response);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchKey")
        {
            throw new FileNotFoundException($"File '{objectKey}' was not found in storage.", objectKey);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "[Tigris] S3 error opening stream {ObjectKey}: {Message}", objectKey, ex.Message);
            throw;
        }
    }

    // =========================================================
    // TASK 4 - Method 4: DeleteFileAsync (bool return)
    // =========================================================

    /// <inheritdoc/>
    public async Task<bool> DeleteFileAsync(string objectKey)
    {
        _logger.LogInformation("Deleting file from Tigris: {ObjectKey}", objectKey);

        try
        {
            await _s3Client.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey
            });

            _logger.LogInformation("File deleted from Tigris: {ObjectKey}", objectKey);
            return true;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete {ObjectKey} from Tigris. It may not exist. Error: {Message}", objectKey, ex.Message);
            return false;
        }
    }

    // =========================================================
    // TASK 4 - Method 5: FileExistsAsync
    // =========================================================

    /// <inheritdoc/>
    public async Task<bool> FileExistsAsync(string objectKey)
    {
        try
        {
            await _s3Client.GetObjectMetadataAsync(_bucketName, objectKey);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound || ex.ErrorCode == "NoSuchKey")
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking existence of {ObjectKey}", objectKey);
            return false;
        }
    }

    // =========================================================
    // TASK 4 - Method 6: GetFileSizeAsync
    // =========================================================

    /// <inheritdoc/>
    public async Task<long> GetFileSizeAsync(string objectKey)
    {
        try
        {
            var metadata = await _s3Client.GetObjectMetadataAsync(_bucketName, objectKey);
            return metadata.ContentLength;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Could not get file size for {ObjectKey}: {Message}", objectKey, ex.Message);
            return 0;
        }
    }

    // =========================================================
    // Legacy byte-array convenience methods (used by MedicalRecordService)
    // =========================================================

    /// <inheritdoc/>
    public async Task<string> UploadEncryptedFileAsync(byte[] encryptedData, string objectKey, string contentType)
    {
        using var stream = new MemoryStream(encryptedData);
        return await UploadFileAsync(stream, objectKey, contentType, objectKey);
    }

    /// <inheritdoc/>
    public async Task<byte[]> DownloadEncryptedFileAsync(string objectKey)
    {
        var stream = await DownloadFileAsync(objectKey);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return ms.ToArray();
    }

    /// <inheritdoc/>
    public async Task<bool> BucketExistsAsync()
    {
        try
        {
            var listResponse = await _s3Client.ListBucketsAsync();
            return listResponse.Buckets.Any(b => b.BucketName == _bucketName);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

}

/// <summary>
/// Wraps the S3 ResponseStream and holds a reference to the GetObjectResponse.
/// This ensures the HTTP connection to S3 stays open as the caller streams through,
/// and is properly closed when the stream is disposed.
/// </summary>
internal sealed class OwningS3Stream(Stream inner, IDisposable owner) : Stream
{
    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() => inner.Flush();
    public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) => inner.ReadAsync(buffer, offset, count, ct);
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) => inner.ReadAsync(buffer, ct);
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    protected override void Dispose(bool disposing)
    {
        if (disposing) { inner.Dispose(); owner.Dispose(); }
        base.Dispose(disposing);
    }
}
