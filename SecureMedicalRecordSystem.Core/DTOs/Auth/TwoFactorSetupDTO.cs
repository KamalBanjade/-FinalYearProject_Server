namespace SecureMedicalRecordSystem.Core.DTOs.Auth;

public class TwoFactorSetupDTO
{
    public Guid UserId { get; set; }
    public string QRCodeUri { get; set; } = string.Empty;
    public string ManualEntryKey { get; set; } = string.Empty;
    public string? Message { get; set; }
}
