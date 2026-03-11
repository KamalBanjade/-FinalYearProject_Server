using SecureMedicalRecordSystem.Core.DTOs.HealthRecords;
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
    
    // NEW properties for Phase 9
    public string ScannerRole { get; set; } = "public";
    public List<string> Permissions { get; set; } = new();
    public List<SuggestedTemplateDTO> SuggestedTemplates { get; set; } = new();
}
