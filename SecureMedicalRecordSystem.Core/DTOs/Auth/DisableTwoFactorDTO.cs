using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.DTOs.Auth;

public class DisableTwoFactorDTO
{
    [Required]
    public string Password { get; set; } = string.Empty;
}
