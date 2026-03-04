using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.DTOs.Auth;

public class VerifyEmailRequestDTO
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string Token { get; set; } = string.Empty;
}
