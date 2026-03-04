using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.DTOs.Auth;

public class ChangePasswordRequestDTO
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare("NewPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
