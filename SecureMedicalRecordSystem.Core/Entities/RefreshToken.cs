namespace SecureMedicalRecordSystem.Core.Entities;

/// <summary>
/// JWT refresh token. Stored server-side to enable revocation.
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public string? ReplacedByToken { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // Navigation
    public ApplicationUser ApplicationUser { get; set; } = null!;
}
