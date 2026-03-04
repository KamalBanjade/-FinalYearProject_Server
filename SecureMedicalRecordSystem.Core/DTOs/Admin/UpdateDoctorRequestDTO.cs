using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.DTOs.Admin;

public class UpdateDoctorRequestDTO
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    [Required]
    public string NMCLicense { get; set; } = string.Empty;

    [Required]
    public string Department { get; set; } = string.Empty;

    [Required]
    public string Specialization { get; set; } = string.Empty;

    public string? QualificationDetails { get; set; }
    
    public bool IsActive { get; set; } = true;
}
