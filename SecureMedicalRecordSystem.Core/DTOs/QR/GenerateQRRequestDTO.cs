namespace SecureMedicalRecordSystem.Core.DTOs.QR;

public class GenerateQRRequestDTO
{
    public int? ExpiryDays { get; set; }
    public string Format { get; set; } = "png"; // "png" or "svg"
}
