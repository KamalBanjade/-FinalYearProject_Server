using System.ComponentModel.DataAnnotations;

namespace SecureMedicalRecordSystem.Core.Entities;

public class Doctor : BaseEntity
{
    public Guid UserId { get; set; }

    // Required fields
    [Required]
    [MaxLength(50)]
    public string NMCLicense { get; set; } = string.Empty;

    [Required]
    public Guid DepartmentId { get; set; }

    // Navigation property
    public Department Department { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Specialization { get; set; } = string.Empty;

    // Optional fields
    public string? QualificationDetails { get; set; }
    public string? HospitalAffiliation { get; set; }
    public string? ContactNumber { get; set; }

    // Cryptography fields for Digital Signatures
    public string? PublicKey { get; set; }
    public string? PrivateKeyEncrypted { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public ICollection<RecordCertification> Certifications { get; set; } = new List<RecordCertification>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<DoctorAvailability> Availabilities { get; set; } = new List<DoctorAvailability>();
}
