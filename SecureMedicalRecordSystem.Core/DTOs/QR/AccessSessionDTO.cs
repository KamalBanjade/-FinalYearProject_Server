using SecureMedicalRecordSystem.Core.Enums;

namespace SecureMedicalRecordSystem.Core.DTOs.QR;

public class AccessSessionDTO
{
    public string SessionToken { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int RemainingMinutes { get; set; }
}
