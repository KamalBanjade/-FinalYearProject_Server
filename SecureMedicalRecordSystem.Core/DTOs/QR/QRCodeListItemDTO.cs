namespace SecureMedicalRecordSystem.Core.DTOs.QR;

public class QRCodeListItemDTO
{
    public string Token { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsExpired { get; set; }
    public int AccessCount { get; set; }
    public DateTime? LastAccessedAt { get; set; }
}
