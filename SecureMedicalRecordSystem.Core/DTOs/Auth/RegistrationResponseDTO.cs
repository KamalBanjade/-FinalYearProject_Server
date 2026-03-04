namespace SecureMedicalRecordSystem.Core.DTOs.Auth;

public class RegistrationResponseDTO
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool RequiresSetup { get; set; }
    
    // QR Code 1 Data (TOTP)
    public string? TOTPSetupQRData { get; set; }
    public string? TOTPSecretManual { get; set; }
    
    // QR Code 2 Data (Medical Access)
    public string? MedicalAccessToken { get; set; }
    public string? MedicalAccessURL { get; set; }
    public DateTime MedicalAccessExpiresAt { get; set; }
}
