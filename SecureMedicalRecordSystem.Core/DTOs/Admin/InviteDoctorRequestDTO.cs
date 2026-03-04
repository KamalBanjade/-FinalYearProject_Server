using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.DTOs.Admin;

public class InviteDoctorRequestDTO
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string NMCLicense { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Department { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Specialization { get; set; } = string.Empty;

    [Required]
    [Phone]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    public string? QualificationDetails { get; set; }
}
