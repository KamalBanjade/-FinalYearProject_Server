using System;

namespace SecureMedicalRecordSystem.Core.DTOs.MedicalRecords;

public class VerificationResultDTO
{
    public bool IsValid { get; set; }
    public string Message { get; set; }
    public bool IsCertified { get; set; }
    public string CertifiedBy { get; set; } // Doctor name
    public DateTime? CertifiedAt { get; set; }
    public string RecordHash { get; set; }
    public string Signature { get; set; }
    public bool HashMatchesCurrentFile { get; set; }
    public string IntegrityStatus { get; set; } // "Valid", "Tampered", "Not Certified"
}
