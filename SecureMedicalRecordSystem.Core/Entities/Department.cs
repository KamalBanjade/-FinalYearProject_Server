using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.Entities;

public class Department : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    
    // Navigation Property back to Doctors
    public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
}
