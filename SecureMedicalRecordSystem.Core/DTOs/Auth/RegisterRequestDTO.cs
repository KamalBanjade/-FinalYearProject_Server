using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.DTOs.Auth;

public class RegisterRequestDTO
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare("Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public DateTime DateOfBirth { get; set; }

    [Required]
    [MaxLength(20)]
    public string Gender { get; set; } = string.Empty;

    [Phone]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }
}
