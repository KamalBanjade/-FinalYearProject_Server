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

    // Optional basic fields
    public string? QualificationDetails { get; set; }
    public string? HospitalAffiliation { get; set; }
    public string? ContactNumber { get; set; }

    // Extended Identity
    public string? Biography { get; set; }
    public int? YearsOfExperience { get; set; }
    public string? ConsultationFee { get; set; }
    public string? ConsultationHours { get; set; }
    public string? ConsultationLocation { get; set; }
    public bool? AcceptsNewPatients { get; set; }

    // JSON-backed structured profile sections
    // Each stores a JSON array of { Title, Institution/Organization, StartYear, EndYear, Description }
    public string? EducationJson { get; set; }
    public string? ExperienceJson { get; set; }
    public string? AwardsJson { get; set; }

    // JSON array of { Name, IssuingBody, Year, Description }
    public string? ProfessionalCertificationsJson { get; set; }

    // JSON array of strings
    public string? ProceduresJson { get; set; }
    public string? LanguagesJson { get; set; }

    // JSON array of { Key, Value } — free-form doctor-defined attributes
    public string? CustomAttributesJson { get; set; }

    // Cryptography fields for Digital Signatures
    public string? PublicKey { get; set; }
    public string? PrivateKeyEncrypted { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public ICollection<RecordCertification> Certifications { get; set; } = new List<RecordCertification>();
    public ICollection<PatientHealthRecord> StructuredRecords { get; set; } = new List<PatientHealthRecord>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<DoctorAvailability> Availabilities { get; set; } = new List<DoctorAvailability>();
}
