namespace SecureMedicalRecordSystem.Core.DTOs.Auth;

public class LoginResponseDTO
{
    public string? Token { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool RequiresTwoFactor { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public bool TOTPSetupCompleted { get; set; }
    public bool RequiresPasswordChange { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? DeviceToken { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? BloodType { get; set; }
    public string? ProfilePictureUrl { get; set; }

    // NEW: Device trust information
    public bool TrustedDevice { get; set; } = false; // Login via trusted device
    public bool CanRememberDevice { get; set; } = false; // Can show checkbox
    public bool DeviceTrusted { get; set; } = false; // Device was just trusted
    
    // NEW: Recovery properties for incomplete setups
    public bool RequiresSetup { get; set; }
    public string? TOTPSetupQRData { get; set; }
    public string? TOTPSecretManual { get; set; }
    public string? MedicalAccessToken { get; set; }
    public string? MedicalAccessURL { get; set; }
    public DateTime? MedicalAccessExpiresAt { get; set; }
}
