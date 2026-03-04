using SecureMedicalRecordSystem.Core.Enums;

namespace SecureMedicalRecordSystem.Core.Entities;

public class QRToken : BaseEntity
{
    public Guid PatientId { get; set; }

    // Required fields
    public string Token { get; set; } = string.Empty;
    public QRTokenType TokenType { get; set; } = QRTokenType.Normal;

    // Access lifecycle
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public int AccessCount { get; set; } = 0;

    // Navigation
    public Patient Patient { get; set; } = null!;
    public ICollection<AccessSession> AccessSessions { get; set; } = new List<AccessSession>();
}
