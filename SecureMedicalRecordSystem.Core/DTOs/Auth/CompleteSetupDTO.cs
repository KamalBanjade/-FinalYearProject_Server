using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.DTOs.Auth;

public class CompleteSetupDTO
{
    [Required]
    public string TOTPCode { get; set; } = string.Empty;

    [Required]
    public bool TOTPScanned { get; set; }

    [Required]
    public bool MedicalQRSaved { get; set; }
}
