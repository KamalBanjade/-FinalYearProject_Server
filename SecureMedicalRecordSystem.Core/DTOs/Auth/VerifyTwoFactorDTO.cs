using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.DTOs.Auth;

public class VerifyTwoFactorDTO
{
    [Required]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be exactly 6 digits.")]
    public string Code { get; set; } = string.Empty;
}
