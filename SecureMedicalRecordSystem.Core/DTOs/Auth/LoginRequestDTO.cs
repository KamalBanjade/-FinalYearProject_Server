using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.DTOs.Auth;

public class LoginRequestDTO
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public string? TwoFactorCode { get; set; }
    
    // NEW: Remember device checkbox
    public bool RememberDevice { get; set; } = false;
    
    // Retaining these as they're required for our FingerprintJS implementation
    public string? DeviceToken { get; set; }
    public DeviceFingerprintDTO? Fingerprint { get; set; }
}
