namespace SecureMedicalRecordSystem.Core.DTOs.QR;

public class VerifyQRCodeRequestDTO
{
    public string Token { get; set; } = string.Empty;
    public string? TotpCode { get; set; }
}
