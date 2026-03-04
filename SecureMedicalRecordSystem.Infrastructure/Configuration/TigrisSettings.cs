namespace SecureMedicalRecordSystem.Infrastructure.Configuration;

/// <summary>
/// Configuration for Tigris S3-compatible object storage.
/// </summary>
public class TigrisSettings
{
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string ServiceUrl { get; set; } = "https://t3.storage.dev";
    public string Region { get; set; } = "auto";
    public string BucketName { get; set; } = string.Empty;
    public bool UsePathStyleAddressing { get; set; } = true;
}
