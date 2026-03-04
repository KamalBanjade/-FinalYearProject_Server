using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.DTOs.Auth;

public class ForgotPasswordRequestDTO
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
